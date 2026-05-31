using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
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
    public async Task<IActionResult> CreateTenant([FromBody] TenantInfo request)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId) || string.IsNullOrWhiteSpace(request.ConnectionString))
        {
            return BadRequest(new { message = "TenantId and ConnectionString are required." });
        }

        var existingTenant = await _masterDbContext.Tenants.FirstOrDefaultAsync(t => t.TenantId == request.TenantId);
        if (existingTenant != null)
        {
            return Conflict(new { message = "Tenant with this ID already exists." });
        }

        // Provider default 'SQLite' set kar do agar pass nahi hua hai
        request.Provider = string.IsNullOrWhiteSpace(request.Provider) ? "SQLite" : request.Provider;

        _masterDbContext.Tenants.Add(request);
        await _masterDbContext.SaveChangesAsync();

        return Ok(new { message = "Tenant configuration created successfully!", tenant = request });
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetAllTenants()
    {
        var tenants = await _masterDbContext.Tenants.ToListAsync();
        return Ok(tenants);
    }

    [HttpPost("{tenantId}/provision")]
    public async Task<IActionResult> ProvisionTenantSchema(string tenantId, [FromQuery] DatabaseProvider provider = DatabaseProvider.SQLite)
    {
        var tenant = await _masterDbContext.Tenants.FirstOrDefaultAsync(t => t.TenantId == tenantId);
        if (tenant == null)
        {
            return NotFound(new { message = "Tenant not found in master database." });
        }

        // Provisioning ke time pe naya provider save karein (Swagger dropdown se)
        tenant.Provider = provider.ToString();
        await _masterDbContext.SaveChangesAsync();

        try
        {
            var tenantOptions = new DbContextOptionsBuilder<AppDbContext>();
            var referenceOptions = new DbContextOptionsBuilder<AppDbContext>();

            // Reference DB hamesha SQLite (vdlcrm.db) rahega, taaki roles copy karne ke liye
            // external DB me dummy/master database maintain na karna pade aur passwords hardcode na hon.
            referenceOptions.UseSqlite("Data Source=vdlcrm.db");

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
                    tenantOptions.UseSqlite(tenant.ConnectionString);
                    break;
            }

            // Temporary resolver inject kar rahe hain taaki DB context ko pata chale ki konsa schema use karna hai
            var tempResolver = new ProvisioningTenantResolver(tenant);
            using var tenantDb = new AppDbContext(tenantOptions.Options, tempResolver);
            
            // EnsureCreatedAsync saari tables (aur schemas Postgres/SQLServer ke case me) bana dega
            await tenantDb.Database.EnsureCreatedAsync(); 

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

            // 4. Default Admin User create karein (Tenant ke name/ID se)
            if (!await tenantDb.Users.AnyAsync(u => u.RoleId == 1)) // RoleId 1 is Admin
            {
                var adminUser = new User
                {
                    Username = tenant.TenantId + "_admin", // Example: vdl_school_1_admin
                    Email = $"admin@{tenant.TenantId}.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"), // Default Password
                    RoleId = 1,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    TenantId = tenant.TenantId
                };

                await tenantDb.Users.AddAsync(adminUser);
                await tenantDb.SaveChangesAsync();

                // Master DB me same admin user insert karein
                var masterAdminUser = new User
                {
                    Username = adminUser.Username,
                    Email = adminUser.Email,
                    PasswordHash = adminUser.PasswordHash,
                    RoleId = adminUser.RoleId,
                    IsActive = adminUser.IsActive,
                    CreatedDate = adminUser.CreatedDate,
                    UpdatedDate = adminUser.UpdatedDate,
                    TenantId = tenant.TenantId
                };
                _masterDbContext.Users.Add(masterAdminUser);
                await _masterDbContext.SaveChangesAsync();
            }

            return Ok(new 
            { 
                message = $"Database schema provisioned successfully for tenant '{tenant.Name}'.",
                databaseUsed = tenant.Provider,
                schemaAssigned = (tenant.Provider.ToLower() == "postgresql" || tenant.Provider.ToLower() == "sqlserver") ? tenant.TenantId.ToLower() : "main (file-based)",
                adminCredentials = new 
                {
                    username = tenant.TenantId + "_admin",
                    password = "Admin@123"
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while provisioning the tenant schema.", error = ex.Message });
        }
    }
}