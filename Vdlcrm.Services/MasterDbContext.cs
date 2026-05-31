using Microsoft.EntityFrameworkCore;
using Vdlcrm.Model;

namespace Vdlcrm.Services;

public class MasterDbContext : DbContext
{
    public MasterDbContext(DbContextOptions<MasterDbContext> options) : base(options)
    {
    }

    // Ye table Master DB me banegi
    public DbSet<TenantInfo> Tenants { get; set; }
    public DbSet<User> Users { get; set; } // Existing User model as Global Table

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.Ignore(e => e.Role); // Master DB me Role table banane se bachne ke liye
        });
    }
}