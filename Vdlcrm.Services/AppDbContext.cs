using Microsoft.EntityFrameworkCore;
using Vdlcrm.Model;

namespace Vdlcrm.Services;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
        });
    }
}
