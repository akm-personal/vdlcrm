using System;
using Xunit;
using Vdlcrm.Services;
using Vdlcrm.Model;
using Microsoft.Extensions.Logging.Abstractions;

namespace Vdlcrm.Tests;

public class PasswordUpdateServiceTests
{
    [Fact]
    public async System.Threading.Tasks.Task UpdatePassword_Succeeds_ForValidTempPassAsync()
    {
        var (context, connection) = TestHelpers.CreateContext();
        try
        {
            // Create user with temporary password
            string tempPassword = "TempPass123!";
            var user = new User
            {
                Username = "tempuser",
                Email = "temp.user@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
                RoleId = 4,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                IsPasswordChangedFromTemp = false
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var logger = new NullLogger<PasswordUpdateService>();
            var service = new PasswordUpdateService(context, logger);

            string newPassword = "NewP@ssw0rd1";

            bool result = await service.UpdatePasswordAsync(user.Id, tempPassword, newPassword);
            Assert.True(result);

            var updatedUser = await context.Users.FindAsync(user.Id);
            Assert.NotNull(updatedUser);
            Assert.True(updatedUser.IsPasswordChangedFromTemp);
            Assert.True(BCrypt.Net.BCrypt.Verify(newPassword, updatedUser.PasswordHash));
        }
        finally
        {
            connection.Close();
        }
    }
}
