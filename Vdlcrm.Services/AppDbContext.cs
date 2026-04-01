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
    }
}
