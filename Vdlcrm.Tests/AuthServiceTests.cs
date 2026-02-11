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

            var authService = new AuthService(context, configuration);

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
}
