using System;
using Xunit;
using Vdlcrm.Services;
using Vdlcrm.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Vdlcrm.Tests;

public class AuthServiceTests
{
    private readonly Xunit.Abstractions.ITestOutputHelper _output;
    public AuthServiceTests(Xunit.Abstractions.ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async System.Threading.Tasks.Task RegisterAndLogin_User_SucceedsAsync()
    {
        var (context, connection) = TestHelpers.CreateContext();
        try
        {
            // Prepare configuration for token generation
            var inMemorySettings = new System.Collections.Generic.Dictionary<string, string>
            {
                {"JwtSettings:SecretKey","this-is-a-very-long-test-secret-key-0123456789abcdef0123456789"},
                {"JwtSettings:Issuer","VdlcrmApi"},
                {"JwtSettings:Audience","VdlcrmUsers"},
                {"JwtSettings:ExpirationMinutes","60"}
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var regLogger = NullLogger<RegistrationService>.Instance;
            var registrationService = new RegistrationService(context, regLogger);
            var authService = new AuthService(context, configuration, registrationService);

            var registerRequest = new Model.DTOs.RegisterRequest
            {
                Username = "newuser",
                Email = "new.user@example.com",
                Password = "P@ssw0rd1",
                RoleId = 4
            };

            var registerResponse = await authService.RegisterAsync(registerRequest);
            _output.WriteLine($"Register response: Success={registerResponse.Success}, Message={registerResponse.Message}");
            Assert.True(registerResponse.Success, registerResponse.Message);

            var loginRequest = new Model.DTOs.LoginRequest
            {
                Username = "newuser",
                Password = "P@ssw0rd1"
            };

            var loginResponse = await authService.LoginAsync(loginRequest);
            _output.WriteLine($"Login response: Success={loginResponse.Success}, Message={loginResponse.Message}");
            Assert.True(loginResponse.Success, loginResponse.Message);
            Assert.False(string.IsNullOrEmpty(loginResponse.Token));
        }
        finally
        {
            connection.Close();
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task Login_With_Username_Email_Mobile_SucceedsAsync()
    {
        var (context, connection) = TestHelpers.CreateContext();
        try
        {
            var inMemorySettings = new System.Collections.Generic.Dictionary<string, string>
            {
                {"JwtSettings:SecretKey","this-is-a-very-long-test-secret-key-0123456789abcdef0123456789"},
                {"JwtSettings:Issuer","VdlcrmApi"},
                {"JwtSettings:Audience","VdlcrmUsers"},
                {"JwtSettings:ExpirationMinutes","60"}
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var regLogger = NullLogger<RegistrationService>.Instance;
            var registrationService = new RegistrationService(context, regLogger);
            var authService = new AuthService(context, configuration, registrationService);

            // 1. Ensure Role Exists
            if (!await context.Roles.AnyAsync(r => r.RoleId == 4))
            {
                context.Roles.Add(new Role { RoleId = 4, RoleName = "Student" });
                await context.SaveChangesAsync();
            }

            // 2. Register User with Username, Email, and Mobile Number
            var registerRequest = new Model.DTOs.RegisterRequest
            {
                Username = "multiuser",
                Email = "multi@example.com",
                MobileNumber = "9876543210",
                Password = "P@ssw0rd1",
                RoleId = 4
            };
            await authService.RegisterAsync(registerRequest);

            // 3. Test Login with Username
            var loginUsername = await authService.LoginAsync(new Model.DTOs.LoginRequest { Username = "multiuser", Password = "P@ssw0rd1" });
            Assert.True(loginUsername.Success, "Login with Username failed");

            // 4. Test Login with Email
            var loginEmail = await authService.LoginAsync(new Model.DTOs.LoginRequest { Username = "multi@example.com", Password = "P@ssw0rd1" });
            Assert.True(loginEmail.Success, "Login with Email failed");

            // 5. Test Login with Mobile Number
            var loginMobile = await authService.LoginAsync(new Model.DTOs.LoginRequest { Username = "9876543210", Password = "P@ssw0rd1" });
            Assert.True(loginMobile.Success, "Login with Mobile failed");
        }
        finally
        {
            connection.Close();
        }
    }
}
