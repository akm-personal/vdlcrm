using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Vdlcrm.Model;

namespace Vdlcrm.Services;

public class AppDbContext : DbContext
{
    private readonly ITenantResolverService? _tenantResolver;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantResolverService? tenantResolver = null) : base(options)
    {
        _tenantResolver = tenantResolver;
    }

    // Helper property to get the current schema for caching
    public string CurrentSchema => (_tenantResolver?.CurrentTenant?.Provider?.ToLower() == "postgresql" || _tenantResolver?.CurrentTenant?.Provider?.ToLower() == "sqlserver") 
        ? _tenantResolver.CurrentTenant.TenantId.ToLower() 
        : "default_schema";

    public DbSet<WeatherForecast> WeatherForecasts { get; set; }
    public DbSet<Student> StudentDetails { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<FeeRecord> FeeRecords { get; set; }
    public DbSet<FeePayment> FeePayments { get; set; }
    public DbSet<Shift> Shifts { get; set; }
    public DbSet<AppStatus> Statuses { get; set; }
    public DbSet<EndpointPermission> EndpointPermissions { get; set; }
    public DbSet<SeatRow> SeatRows { get; set; }
    public DbSet<Seat> Seats { get; set; }
    public DbSet<SeatAssignment> SeatAssignments { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<StudentDocument> StudentDocuments { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }
    public DbSet<AttendanceRecord> AttendanceRecords { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Replace default model cache factory with our tenant-aware factory
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>();

        // Agar request kisi specific tenant ke liye aayi hai
        if (_tenantResolver?.CurrentTenant != null)
        {
            var provider = _tenantResolver.CurrentTenant.Provider.ToLower();
            
            if (provider == "postgresql")
            {
                 optionsBuilder.UseNpgsql(_tenantResolver.CurrentTenant.ConnectionString);
            }
            else if (provider == "sqlserver")
            {
                 optionsBuilder.UseSqlServer(_tenantResolver.CurrentTenant.ConnectionString);
            }
            else
            {
                optionsBuilder.UseSqlite(_tenantResolver.CurrentTenant.ConnectionString);
            }
        }
        // Fallback: Agar connection string specify nahi hui (e.g. Migration chalate waqt)
        else if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=main_database/vdlcrm.db");
        }
        
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Postgres aur SQL Server ke liye dynamically default schema set karein
        if (_tenantResolver?.CurrentTenant != null)
        {
            var provider = _tenantResolver.CurrentTenant.Provider.ToLower();
            if (provider == "postgresql" || provider == "sqlserver")
            {
                modelBuilder.HasDefaultSchema(_tenantResolver.CurrentTenant.TenantId.ToLower());
            }
        }

        // Configure WeatherForecast entity
        modelBuilder.Entity<WeatherForecast>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.TemperatureC).IsRequired();
            entity.Property(e => e.Summary).HasMaxLength(50);
        });

        // Configure Student entity with table name
        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("student_details");
            entity.HasKey(e => e.Id);
            entity.HasAlternateKey(e => e.VdlId);
            entity.Property(e => e.Id).HasColumnOrder(0);
            entity.Property(e => e.VdlId).IsRequired().HasMaxLength(50).HasColumnOrder(1);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100).HasColumnOrder(2);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100).HasColumnOrder(3);
            entity.Property(e => e.FatherName).IsRequired(false).HasMaxLength(100).HasColumnOrder(4);
            entity.Property(e => e.Gender).IsRequired(false).HasMaxLength(20).HasColumnOrder(5);
            entity.Property(e => e.SeatNumber).IsRequired(false).HasColumnOrder(6);
            entity.Property(e => e.ShiftType).IsRequired(false).HasMaxLength(50).HasColumnOrder(7);
            entity.Property(e => e.Address).IsRequired(false).HasMaxLength(255).HasColumnOrder(8);
            entity.Property(e => e.AlternateNumber).HasMaxLength(20).HasColumnOrder(9);
            entity.Property(e => e.Class).IsRequired(false).HasMaxLength(50).HasColumnOrder(10);
            entity.Property(e => e.DateOfBirth).IsRequired(false).HasColumnOrder(11);
            entity.Property(e => e.IdProof).IsRequired(false).HasMaxLength(100).HasColumnOrder(12);
            entity.Property(e => e.MobileNumber).IsRequired(false).HasMaxLength(20).HasColumnOrder(13);
            entity.Property(e => e.StudentStatus).IsRequired(false).HasMaxLength(50).HasColumnOrder(14);
            entity.Property(e => e.CreatedDate).IsRequired().HasColumnOrder(15);
            entity.Property(e => e.UpdatedDate).IsRequired().HasColumnOrder(16);
        });

        // Configure Role entity with table name
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(e => e.RoleSequenceId);
            entity.Property(e => e.RoleName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RoleId).IsRequired();
            entity.HasIndex(e => e.RoleId).IsUnique();
        });

        // Configure User entity with table name
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.MobileNumber).HasMaxLength(20);
            entity.HasIndex(e => e.MobileNumber).IsUnique();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.RoleId).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedDate).IsRequired();
            entity.Property(e => e.UpdatedDate).IsRequired();
            
            // Foreign key relationship
            entity.HasOne(e => e.Role)
                .WithMany()
                .HasForeignKey(e => e.RoleId)
                .HasPrincipalKey(r => r.RoleId);
        });

        // Configure Shift entity with table name
        modelBuilder.Entity<Shift>(entity =>
        {
            entity.ToTable("shifts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ShiftName).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.ShiftName).IsUnique();
            entity.Property(e => e.Status).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.StartTime).HasMaxLength(20);
            entity.Property(e => e.EndTime).HasMaxLength(20);
            entity.Property(e => e.CreatedBy).IsRequired();
            entity.Property(e => e.UpdatedBy).IsRequired(false);
            entity.Property(e => e.CreatedDate).IsRequired();
            entity.Property(e => e.UpdatedDate).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
        });

        // Configure FeeRecord entity
        modelBuilder.Entity<FeeRecord>(entity =>
        {
            entity.ToTable("fee_records");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.TotalFee).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().IsRequired().HasMaxLength(20);
            entity.Property(e => e.Description).IsRequired(false);
            
            entity.HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.VdlId)
                .HasPrincipalKey(s => s.VdlId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure FeePayment entity
        modelBuilder.Entity<FeePayment>(entity =>
        {
            entity.ToTable("fee_payments");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.AmountPaid).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.PaymentMode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Note).IsRequired(false);
            
            entity.HasOne(e => e.FeeRecord)
                .WithMany(f => f.FeePayments)
                .HasForeignKey(e => e.FeeRecordId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Status entity
        modelBuilder.Entity<AppStatus>(entity =>
        {
            entity.ToTable("statuses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.StatusType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.StatusName).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => new { e.StatusType, e.StatusName }).IsUnique();
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);

            // Seed default statuses into the database
            entity.HasData(
                // Fee Statuses
                new AppStatus { Id = 1, StatusId = 1, StatusType = "Fee", StatusName = "Pending", IsActive = true },
                new AppStatus { Id = 2, StatusId = 2, StatusType = "Fee", StatusName = "Partial", IsActive = true },
                new AppStatus { Id = 3, StatusId = 3, StatusType = "Fee", StatusName = "Paid", IsActive = true },
                
                // General Statuses (For Shift, User, etc.)
                new AppStatus { Id = 4, StatusId = 4, StatusType = "General", StatusName = "Active", IsActive = true },
                new AppStatus { Id = 5, StatusId = 5, StatusType = "General", StatusName = "Not Active", IsActive = true },

                // Student Statuses
                new AppStatus { Id = 6, StatusId = 6, StatusType = "Student", StatusName = "Active", IsActive = true },
                new AppStatus { Id = 7, StatusId = 7, StatusType = "Student", StatusName = "Not Active", IsActive = true },
                new AppStatus { Id = 8, StatusId = 8, StatusType = "Student", StatusName = "Dropped", IsActive = true },
                new AppStatus { Id = 9, StatusId = 9, StatusType = "Student", StatusName = "Cancelled", IsActive = true }
            );
        });

        // Configure EndpointPermission entity
        modelBuilder.Entity<EndpointPermission>(entity =>
        {
            entity.ToTable("endpoint_permissions");
            entity.HasKey(e => e.Id);
        });
        
        // Seed default application settings
        modelBuilder.Entity<AppSetting>().HasData(
            new AppSetting { Key = "LibraryLatitude", Value = "28.6139", Description = "Library exact latitude (Default: Delhi)", UpdatedAt = DateTime.UtcNow },
            new AppSetting { Key = "LibraryLongitude", Value = "77.2090", Description = "Library exact longitude", UpdatedAt = DateTime.UtcNow },
            new AppSetting { Key = "AttendanceRadius", Value = "50", Description = "Allowed radius for punch in/out in meters", UpdatedAt = DateTime.UtcNow },
            new AppSetting { Key = "AutoPunchOutHours", Value = "8", Description = "Hours after which a student is auto-punched out", UpdatedAt = DateTime.UtcNow },
            new AppSetting { Key = "AutoPunchOutWorkerEnabled", Value = "true", Description = "Enable background auto punch out job (true/false)", UpdatedAt = DateTime.UtcNow },
            new AppSetting { Key = "AutoPunchOutWorkerIntervalHours", Value = "5", Description = "How often background job runs (hours)", UpdatedAt = DateTime.UtcNow },
            new AppSetting { Key = "AutoPunchOutWorkerMode", Value = "Day", Description = "When to run (Day/Night/Both)", UpdatedAt = DateTime.UtcNow },
            new AppSetting { Key = "AutoPunchOutDayStart", Value = "08:00", Description = "Day shift start time (HH:mm)", UpdatedAt = DateTime.UtcNow },
            new AppSetting { Key = "AutoPunchOutDayEnd", Value = "20:00", Description = "Day shift end time (HH:mm)", UpdatedAt = DateTime.UtcNow }
        );

        // Configure SeatRow entity
        modelBuilder.Entity<SeatRow>(entity =>
        {
            entity.ToTable("seat_rows");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RowName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RowOrder).IsRequired();
            entity.Property(e => e.IsLocked).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
        });

        // Configure Seat entity
        modelBuilder.Entity<Seat>(entity =>
        {
            entity.ToTable("seats");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SeatLabel).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IsLocked).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);

            entity.HasOne(e => e.SeatRow)
                .WithMany(r => r.Seats)
                .HasForeignKey(e => e.SeatRowId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure SeatAssignment entity
        modelBuilder.Entity<SeatAssignment>(entity =>
        {
            entity.ToTable("seat_assignments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);

            entity.HasOne(e => e.Seat)
                .WithMany(s => s.SeatAssignments)
                .HasForeignKey(e => e.SeatId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentVdlId)
                .HasPrincipalKey(s => s.VdlId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Shift)
                .WithMany()
                .HasForeignKey(e => e.ShiftId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure StudentDocument entity
        modelBuilder.Entity<StudentDocument>(entity =>
        {
            entity.ToTable("student_documents");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.VdlId); // VdlId par index taaki search fast ho
        });
    }
}

// Custom Cache Key Factory: Ye ensure karega ki EF Core har tenant schema ka alag model cache banaye
public class TenantModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        var dbContext = context as AppDbContext;
        var schema = dbContext?.CurrentSchema ?? "default_schema";
        return (context.GetType(), schema, designTime);
    }
}
