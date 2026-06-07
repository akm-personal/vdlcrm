using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Vdlcrm.Model;
using Vdlcrm.Model.DTOs;

namespace Vdlcrm.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly RegistrationService _registrationService;

    public AuthService(AppDbContext context, IConfiguration configuration, RegistrationService registrationService)
    {
        _context = context;
        _configuration = configuration;
        _registrationService = registrationService;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            // Find user by username, email, or mobile number
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Username || (u.MobileNumber != null && u.MobileNumber == request.Username));

            if (user == null || !user.IsActive)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid credentials or user is not active"
                };
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid password"
                };
            }

            // StudentDetails table se Name fetch karein
            var studentName = await _context.StudentDetails
                .Where(s => s.Email == user.Email || s.VdlId == user.Username)
                .Select(s => s.Name)
                .FirstOrDefaultAsync();

            // Generate JWT token
            var token = GenerateJwtToken(user);

            return new LoginResponse
            {
                Success = true,
                Message = "Login successful",
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    Name = studentName,
                    RoleId = user.RoleId,
                    RoleName = user.Role?.RoleName ?? "Unknown",
                    IsActive = user.IsActive
                }
            };
        }
        catch (Exception ex)
        {
            return new LoginResponse
            {
                Success = false,
                Message = $"Login failed: {ex.Message}"
            };
        }
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request, string createdBy = "self")
    {
        try
        {
            // Check if user already exists
            var existingEmailUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingEmailUser != null)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Email already exists"
                };
            }

            // Check if mobile number already exists (if provided)
            if (!string.IsNullOrEmpty(request.MobileNumber))
            {
                var existingMobileUser = await _context.Users.FirstOrDefaultAsync(u => u.MobileNumber == request.MobileNumber);
                if (existingMobileUser != null)
                {
                    return new LoginResponse { Success = false, Message = "Mobile number already exists" };
                }
            }

            // Validate role exists
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleId == request.RoleId);

            if (role == null)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = "Invalid role"
                };
            }

            // Generate VDL ID using RegistrationService
            string generatedVdlId = await _registrationService.GenerateVdlIdAsync();

            // Create an empty student record with just Name and Email
            var newStudent = new Student
            {
                VdlId = generatedVdlId,
                CreatedBy = createdBy,
                Name = request.Username, // Frontend username is saved as Student Name
                Email = request.Email,
                MobileNumber = request.MobileNumber,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.StudentDetails.Add(newStudent);

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            // Create new user
            var newUser = new User
            {
                Username = generatedVdlId, // Set Username as generated VDL ID
                Email = request.Email,
                MobileNumber = request.MobileNumber,
                PasswordHash = passwordHash,
                RoleId = request.RoleId,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return new LoginResponse
            {
                Success = true,
                Message = "User registered successfully",
                User = new UserDto
                {
                    Id = newUser.Id,
                    Username = newUser.Username,
                    Email = newUser.Email,
                    Name = newStudent.Name,
                    RoleId = newUser.RoleId,
                    RoleName = role.RoleName,
                    IsActive = newUser.IsActive
                }
            };
        }
        catch (Exception ex)
        {
            return new LoginResponse
            {
                Success = false,
                Message = $"Registration failed: {ex.InnerException?.Message ?? ex.Message}"
            };
        }
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey ?? "default-secret-key-that-is-long-enough"));

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("RoleId", user.RoleId.ToString()),
            new Claim("RoleName", user.Role?.RoleName ?? "Unknown"),
            new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Unknown")
        };

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<Role?> GetRoleAsync(int roleId)
    {
        return await _context.Roles.FirstOrDefaultAsync(r => r.RoleId == roleId);
    }

    public async Task<List<Role>> GetAllRolesAsync()
    {
        return await _context.Roles.ToListAsync();
    }
}
