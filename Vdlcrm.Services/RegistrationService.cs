using System.Text;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vdlcrm.Model;

namespace Vdlcrm.Services;

/// <summary>
/// Service for handling bulk student registration with auto-generated IDs and user accounts
/// Handles concurrent registrations safely with transactions
/// </summary>
public class RegistrationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<RegistrationService> _logger;
    private static readonly object _vdlIdLock = new object(); // Lock for VDL ID generation

    public RegistrationService(AppDbContext context, ILogger<RegistrationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Generate next available VDL ID (VDL001, VDL002, etc.) - Thread-safe
    /// </summary>
    /// <returns>Next available VDL ID</returns>
    public async Task<string> GenerateVdlIdAsync()
    {
        // Use lock to ensure thread-safe VDL ID generation
        lock (_vdlIdLock)
        {
            try
            {
                // Get the highest existing VDL ID
                // VDL IDs are in format: VDL###
                var lastStudent = _context.StudentDetails
                    .AsNoTracking()
                    .OrderByDescending(s => s.VdlId)
                    .FirstOrDefault();

                int nextNumber = 1;

                if (lastStudent != null && !string.IsNullOrEmpty(lastStudent.VdlId))
                {
                    var vdlId = lastStudent.VdlId.ToUpper(); // Case-insensitive
                    
                    // Extract number from VDL###
                    if (vdlId.StartsWith("VDL") && int.TryParse(vdlId.Substring(3), out int currentNumber))
                    {
                        nextNumber = currentNumber + 1;
                    }
                }

                // Generate new VDL ID with padding (VDL001, VDL002, VDL999, VDL1000, etc.)
                string newVdlId = $"VDL{nextNumber:D3}";

                _logger.LogInformation($"Generated VDL ID: {newVdlId}");
                return newVdlId;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating VDL ID: {ex.Message}");
                throw new InvalidOperationException("Failed to generate VDL ID", ex);
            }
        }
    }

    /// <summary>
    /// Generate a temporary password
    /// Format: TempPass_YYYYMMDD_Random6Chars
    /// Example: TempPass_20260209_aB3cD9
    /// </summary>
    /// <returns>Temporary password</returns>
    public string GenerateTempPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%";
        var random = new Random();
        var passwordBuilder = new StringBuilder();

        // Base: TempPass_Date_Random
        string dateStr = DateTime.Now.ToString("yyyyMMdd");
        
        // Add random characters for security
        for (int i = 0; i < 8; i++)
        {
            passwordBuilder.Append(chars[random.Next(chars.Length)]);
        }

        string tempPassword = $"Temp{dateStr}_{passwordBuilder.ToString()}";
        
        _logger.LogInformation($"Generated temporary password");
        return tempPassword;
    }

    /// <summary>
    /// Create user account for registered student
    /// </summary>
    /// <param name="student">Student details</param>
    /// <param name="tempPassword">Temporary password generated</param>
    /// <returns>Created user or null if failed</returns>
    public async Task<User?> CreateUserAccountAsync(Student student, string tempPassword)
    {
        try
        {
            // Validate student data
            if (student == null || string.IsNullOrEmpty(student.Email))
            {
                throw new ArgumentException("Student and email are required");
            }

            // Use VDL ID as the username
            string username = student.VdlId.ToUpper();

            // Create user account with Role ID 4 (Student)
            var user = new User
            {
                Username = username,
                Email = student.Email.ToLower(), // Case-insensitive email
                MobileNumber = student.MobileNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
                RoleId = 4, // Student role
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created user account for student: {student.VdlId}, Username: {username}");
            return user;
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError($"Database error creating user account: {dbEx.Message}");
            // User might already exist, try to fetch and return it
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == student.Email.ToLower());
            
            if (existingUser != null)
            {
                _logger.LogInformation($"User already exists for email: {student.Email}");
                return existingUser;
            }
            
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating user account: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Register student with transaction - handles VDL ID generation and user creation
    /// Safe for concurrent/bulk registrations
    /// </summary>
    /// <param name="student">Student to register</param>
    /// <param name="tempPasswordOutput">Output temp password</param>
    /// <returns>Registered student with VDL ID</returns>
    public async Task<(Student student, User user, string tempPassword)> RegisterStudentWithUserAsync(Student student)
    {
        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                // 1. Generate VDL ID
                if (string.IsNullOrEmpty(student.VdlId))
                {
                    student.VdlId = await GenerateVdlIdAsync();
                }

                // 2. Set audit fields
                student.CreatedDate = DateTime.UtcNow;
                student.UpdatedDate = DateTime.UtcNow;

                // 3. Set default status
                if (string.IsNullOrEmpty(student.StudentStatus))
                {
                    student.StudentStatus = "Active";
                }

                // 4. Save student to database
                _context.StudentDetails.Add(student);
                await _context.SaveChangesAsync();

                // 5. Generate temporary password
                string tempPassword = GenerateTempPassword();

                // 6. Create user account for student
                var user = await CreateUserAccountAsync(student, tempPassword);

                // 7. Commit transaction
                await transaction.CommitAsync();

                _logger.LogInformation($"Successfully registered student {student.VdlId} with user account {user?.Username}");
                return (student, user!, tempPassword);
            }
            catch (Exception ex)
            {
                // Rollback on any error
                await transaction.RollbackAsync();
                _logger.LogError($"Error in student registration transaction: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// Get next available user ID (if needed for other purposes)
    /// </summary>
    /// <returns>Next available user ID</returns>
    public async Task<int> GetNextUserIdAsync()
    {
        var maxId = await _context.Users
            .AsNoTracking()
            .MaxAsync(u => (int?)u.Id) ?? 0;

        return maxId + 1;
    }

    /// <summary>
    /// Check if VDL ID already exists
    /// </summary>
    /// <param name="vdlId">VDL ID to check</param>
    /// <returns>True if exists, false otherwise</returns>
    public async Task<bool> VdlIdExistsAsync(string vdlId)
    {
        return await _context.StudentDetails
            .AsNoTracking()
            .AnyAsync(s => s.VdlId.ToUpper() == vdlId.ToUpper());
    }
}
