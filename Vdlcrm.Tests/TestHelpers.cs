using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Vdlcrm.Services;
using Vdlcrm.Model;

namespace Vdlcrm.Tests;

public static class TestHelpers
{
    public static (AppDbContext Context, SqliteConnection Connection) CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        if (!context.Roles.Any())
        {
            context.Roles.Add(new Role { RoleSequenceId = 1, RoleId = 1, RoleName = "Admin" });
            context.Roles.Add(new Role { RoleSequenceId = 2, RoleId = 2, RoleName = "Internal" });
            context.Roles.Add(new Role { RoleSequenceId = 3, RoleId = 3, RoleName = "External" });
            context.Roles.Add(new Role { RoleSequenceId = 4, RoleId = 4, RoleName = "Student" });
            context.SaveChanges();
        }

        return (context, connection);
    }
}
