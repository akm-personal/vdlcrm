using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System;
using Vdlcrm.Model;
using Vdlcrm.Model.DTOs;
using Vdlcrm.Services;

namespace Vdlcrm.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
// [Authorize(Roles = "Admin")] // Aap isko baad me Admin role ke sath secure kar sakte hain
public class TenantController : ControllerBase
{
    private readonly MasterDbContext _masterDbContext;

    // Temporary resolver class to pass current tenant context during provisioning
    private class ProvisioningTenantResolver : ITenantResolverService
    {
        public TenantInfo? CurrentTenant { get; }
        public ProvisioningTenantResolver(TenantInfo tenant)
        {
            CurrentTenant = tenant;
        }
    }

    public TenantController(MasterDbContext masterDbContext)
    {
        _masterDbContext = masterDbContext;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateTenant([FromBody] TenantCreateRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Name is required." });
            }

            // Create a new TenantInfo object from the request DTO
            var tenantInfo = new TenantInfo
            {
                Name = request.Name,
                ConnectionString = request.ConnectionString ?? "",
                Provider = string.IsNullOrWhiteSpace(request.Provider) ? "SQLite" : request.Provider
            };

            // 1. Generate Initials from Name (e.g., "Vinayak Digital Library" -> "vdl")
            var words = tenantInfo.Name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var initials = string.Join("", words.Select(w => w[0])).ToLower();
            initials = new string(initials.Where(char.IsLetterOrDigit).ToArray()); // Sanitize for safety
            if (string.IsNullOrEmpty(initials)) initials = "tenant";

            // 2. Auto-generate TenantId with GLOBAL sequence (e.g., 001, 002, 003...)
            // Find the highest sequence number currently in use across ALL tenants
            var existingTenantIds = await _masterDbContext.Tenants.Select(t => t.TenantId).ToListAsync();
            int maxSequence = 0;
            foreach (var id in existingTenantIds)
            {
                var parts = id.Split('_');
                if (parts.Length > 0 && int.TryParse(parts.Last(), out int parsedSeq))
                {
                    if (parsedSeq > maxSequence)
                    {
                        maxSequence = parsedSeq;
                    }
                }
            }

            int sequence = maxSequence + 1;
            string newTenantId;
            do
            {
                newTenantId = $"{initials}_{tenantInfo.Provider.ToLower()}_{sequence:D3}";
                sequence++;
            } while (await _masterDbContext.Tenants.AnyAsync(t => t.TenantId == newTenantId));

            tenantInfo.TenantId = newTenantId;

            // Server-based DBs ke liye ConnectionString required hai
            if (tenantInfo.Provider.ToLower() != "sqlite" && string.IsNullOrWhiteSpace(tenantInfo.ConnectionString))
            {
                return BadRequest(new { message = "ConnectionString is required for PostgreSQL or SQLServer providers." });
            }

            // SQLite ke liye connection string ko khali save karein, ye provision me banega
            if (tenantInfo.Provider.ToLower() == "sqlite")
            {
                tenantInfo.ConnectionString = ""; // Set to empty
            }
            _masterDbContext.Tenants.Add(tenantInfo);
            await _masterDbContext.SaveChangesAsync();

            return Ok(new { message = "Tenant configuration created successfully! Please provision it next.", tenant = tenantInfo });
        }
        catch (Exception ex)
        {
            var originalError = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, new { message = "An error occurred while creating the tenant.", error = originalError, details = ex.ToString() });
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetAllTenants()
    {
        try
        {
            var tenants = await _masterDbContext.Tenants.ToListAsync();
            return Ok(tenants);
        }
        catch (Exception ex)
        {
            var originalError = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, new { message = "An error occurred while fetching tenants.", error = originalError, details = ex.ToString() });
        }
    }

    [HttpDelete("reset-master")]
    public async Task<IActionResult> ResetMasterDatabase()
    {
        try
        {
            // 1. Direct SQL commands se dono tables ka saara data delete karein
            await _masterDbContext.Database.ExecuteSqlRawAsync("DELETE FROM Tenants;");
            await _masterDbContext.Database.ExecuteSqlRawAsync("DELETE FROM users;");

            // 2. SQLite sequences reset karein taaki IDs wapas 1, 2, 3.. se start hon
            await _masterDbContext.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence WHERE name IN ('Tenants', 'users', 'Users');");

            return Ok(new { message = "Master database (Tenants and Users tables) cleared and sequence reset successfully." });
        }
        catch (Exception ex)
        {
            var originalError = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, new { message = "An error occurred while resetting the master database.", error = originalError, details = ex.ToString() });
        }
    }

    [HttpPost("{tenantId}/provision")]
    public async Task<IActionResult> ProvisionTenantSchema(string tenantId, [FromQuery] DatabaseProvider provider = DatabaseProvider.SQLite)
    {
        try
        {
            // TenantId ko clean aur lowercase karein taaki master db se search me exact match ho
            tenantId = tenantId.Trim().ToLower();

            var tenant = await _masterDbContext.Tenants.FirstOrDefaultAsync(t => t.TenantId == tenantId);
            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found in master database." });
            }

            var tenantOptions = new DbContextOptionsBuilder<AppDbContext>();
            var referenceOptions = new DbContextOptionsBuilder<AppDbContext>();

            // 1. Auto-Backup Trigger: Provisioning se pehle main databases ka backup le lein
            CreateBackupOfMainDatabases();

            // Reference DB hamesha SQLite (vdlcrm.db) rahega, taaki roles copy karne ke liye
            // external DB me dummy/master database maintain na karna pade aur passwords hardcode na hon.
            referenceOptions.UseSqlite("Data Source=main_database/vdlcrm.db");

            // Base directory for all tenant data
            var baseDirectory = Path.Combine("main_database", "tenants_database");
            Directory.CreateDirectory(baseDirectory);

            // Provider ke hisab se sirf Tenant DB set karein
            switch (tenant.Provider.ToLower())
            {
                case "postgresql":
                    tenantOptions.UseNpgsql(tenant.ConnectionString);
                    break;

                case "sqlserver":
                    tenantOptions.UseSqlServer(tenant.ConnectionString);
                    break;

                case "sqlite":
                default:
                    tenant.Provider = "SQLite"; // default enforce

                    // Organize SQLite DBs into folders
                    var tenantDbDirectory = Path.Combine(baseDirectory, tenant.TenantId);
                    Directory.CreateDirectory(tenantDbDirectory);
                    var dbPath = Path.Combine(tenantDbDirectory, $"{tenant.TenantId}.db");
                    tenant.ConnectionString = $"Data Source={dbPath}"; // Update connection string

                    tenantOptions.UseSqlite(tenant.ConnectionString);
                    break;
            }

            // Temporary resolver inject kar rahe hain taaki DB context ko pata chale ki konsa schema use karna hai
            var tempResolver = new ProvisioningTenantResolver(tenant);
            
            await _masterDbContext.SaveChangesAsync(); // Save updated provider and connection string
            using var tenantDb = new AppDbContext(tenantOptions.Options, tempResolver);
            
            // EnsureCreatedAsync skips schema creation in Multi-Tenant DBs if another schema already exists.
            // Therefore, for Postgres/SQLServer, we explicitly call CreateTablesAsync.
            if (tenant.Provider.ToLower() == "sqlite")
            {
                await tenantDb.Database.EnsureCreatedAsync(); 
            }
            else
            {
                var dbCreator = tenantDb.GetService<IRelationalDatabaseCreator>();
                if (!await dbCreator.ExistsAsync())
                {
                    await dbCreator.CreateAsync();
                }
                try
                {
                    await dbCreator.CreateTablesAsync();
                }
                catch
                {
                    // Safe to ignore: Throws if tables/schema already exist (idempotent safe)
                }
            }

            // 2. Master App DB (vdlcrm.db) se connect karke roles fetch karein
            using var referenceDb = new AppDbContext(referenceOptions.Options, null);
            var defaultRoles = await referenceDb.Roles.AsNoTracking().ToListAsync();

            // 3. Roles ko naye Tenant DB me copy karein
            if (defaultRoles.Any() && !await tenantDb.Roles.AnyAsync())
            {
                var rolesToInsert = defaultRoles.Select(r => new Role
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName
                }).ToList();

                await tenantDb.Roles.AddRangeAsync(rolesToInsert);
                await tenantDb.SaveChangesAsync();
            }

            // 4. Custom aur clean Admin Username aur Email generate karein
            var parts = tenant.TenantId.Split('_');
            string initials = parts.Length > 0 ? parts[0] : "tenant";
            string seq = parts.Length > 1 ? parts.Last() : "001";
            
            string defaultAdminUsername = $"{initials}admin{seq}"; // e.g., vdladmin001
            string defaultAdminEmail = $"{defaultAdminUsername}@{initials}{seq}.com"; // e.g., vdladmin001@vdl001.com

            // 1. Tenant DB me check karein aur insert karein
            var tenantAdminUser = await tenantDb.Users.FirstOrDefaultAsync(u => u.RoleId == 1);
            if (tenantAdminUser == null)
            {
                tenantAdminUser = new User
                {
                    Username = defaultAdminUsername,
                    Email = defaultAdminEmail,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"), // Default Password
                    RoleId = 1,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    TenantId = tenant.TenantId
                };

                await tenantDb.Users.AddAsync(tenantAdminUser);
                await tenantDb.SaveChangesAsync();
            }

            // 2. Master DB me ensure karein (Agar Master reset ho gaya ho, toh yahan WAPAS sync ho jayega!)
            var masterAdminUser = await _masterDbContext.Users.FirstOrDefaultAsync(u => u.TenantId == tenant.TenantId && u.RoleId == 1);
            if (masterAdminUser == null)
            {
                masterAdminUser = new User
                {
                    Username = tenantAdminUser.Username,
                    Email = tenantAdminUser.Email,
                    PasswordHash = tenantAdminUser.PasswordHash,
                    RoleId = tenantAdminUser.RoleId,
                    IsActive = tenantAdminUser.IsActive,
                    CreatedDate = tenantAdminUser.CreatedDate,
                    UpdatedDate = tenantAdminUser.UpdatedDate,
                    TenantId = tenant.TenantId
                };
                _masterDbContext.Users.Add(masterAdminUser);
                await _masterDbContext.SaveChangesAsync();
            }

            var responsePayload = new
            { 
                message = $"Database schema provisioned successfully for tenant '{tenant.Name}'.",
                databaseUsed = tenant.Provider,
                schemaAssigned = (tenant.Provider.ToLower() == "postgresql" || tenant.Provider.ToLower() == "sqlserver") ? tenant.TenantId.ToLower() : tenant.ConnectionString,
                adminCredentials = new 
                {
                    username = tenantAdminUser.Username,
                    password = "Admin@123"
                }
            };

            // Create receipt file for server-based DBs
            if (tenant.Provider.ToLower() != "sqlite")
            {
                var tenantReceiptDirectory = Path.Combine(baseDirectory, tenant.TenantId);
                Directory.CreateDirectory(tenantReceiptDirectory);
                var receiptPath = Path.Combine(tenantReceiptDirectory, $"{tenant.TenantId}.txt");
                var jsonResponse = System.Text.Json.JsonSerializer.Serialize(responsePayload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(receiptPath, jsonResponse);
            }

            return Ok(responsePayload);
        }
        catch (Exception ex)
        {
            var originalError = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, new { message = "An error occurred while provisioning the tenant schema.", error = originalError, details = ex.ToString() });
        }
    }

    // Helper method to create automatic backups of main databases
    private void CreateBackupOfMainDatabases()
    {
        try
        {
            var backupDir = "backup_database";
            Directory.CreateDirectory(backupDir);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            var mainDbPath = Path.Combine("main_database", "vdlcrm.db");
            if (System.IO.File.Exists(mainDbPath))
            {
                System.IO.File.Copy(mainDbPath, Path.Combine(backupDir, $"vdlcrm_backup_{timestamp}.db"), true);
            }

            var masterDbPath = Path.Combine("main_database", "vdlcrm_master.db");
            if (System.IO.File.Exists(masterDbPath))
            {
                System.IO.File.Copy(masterDbPath, Path.Combine(backupDir, $"vdlcrm_master_backup_{timestamp}.db"), true);
            }
        }
        catch { /* Backup fail hone par API crash nahi karni chahiye */ }
    }

    [HttpPost("sync-roles")]
    public async Task<IActionResult> SyncRolesToMaster()
    {
        try
        {
            // EF Core migrations ka chakkar chhod kar, direct SQLite query se table aur data copy!
            await _masterDbContext.Database.ExecuteSqlRawAsync("ATTACH DATABASE 'main_database/vdlcrm.db' AS SourceDb;");
            await _masterDbContext.Database.ExecuteSqlRawAsync("CREATE TABLE IF NOT EXISTS roles AS SELECT * FROM SourceDb.roles WHERE 1=0;"); // Exact column structure copy karta hai
            await _masterDbContext.Database.ExecuteSqlRawAsync("INSERT OR IGNORE INTO roles SELECT * FROM SourceDb.roles;"); // Saara data copy karta hai
            await _masterDbContext.Database.ExecuteSqlRawAsync("DETACH DATABASE SourceDb;");
            
            return Ok(new { message = "Roles table and data directly copied to master database successfully!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to sync roles.", error = ex.Message });
        }
    }

    [HttpPost("fix-seat-table")]
    public async Task<IActionResult> FixSeatTable()
    {
        try
        {
            // Sirf seat_assignments table ko drop karke sahi schema ke sath recreate karenge
            // Baaki pura database (students, users, fees, seats) ekdum safe rahega!
            var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=main_database/vdlcrm.db").Options;
            using var db = new AppDbContext(options, null);

            await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS seat_assignments;");
            
            var createTableSql = @"
                CREATE TABLE seat_assignments (
                    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    SeatId INTEGER NOT NULL,
                    ShiftId INTEGER NOT NULL,
                    StudentVdlId TEXT NOT NULL,
                    CreatedBy TEXT,
                    IsDeleted INTEGER NOT NULL DEFAULT 0,
                    AssignedDate TEXT NOT NULL,
                    RemovedDate TEXT,
                    FOREIGN KEY(SeatId) REFERENCES seats(Id) ON DELETE CASCADE,
                    FOREIGN KEY(ShiftId) REFERENCES shifts(Id) ON DELETE CASCADE,
                    FOREIGN KEY(StudentVdlId) REFERENCES student_details(VdlId) ON DELETE CASCADE
                );";

            await db.Database.ExecuteSqlRawAsync(createTableSql);
            return Ok(new { message = "seat_assignments table fixed successfully! Database was NOT deleted." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to fix table", error = ex.Message });
        }
    }
}