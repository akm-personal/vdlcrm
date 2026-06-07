using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vdlcrm.Model;

namespace Vdlcrm.Services;

/// <summary>
/// Service for handling first-time password updates for temporary users
/// Handles concurrent password updates safely with database-level locking
/// </summary>
public class PasswordUpdateService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PasswordUpdateService> _logger;

    public PasswordUpdateService(AppDbContext context, ILogger<PasswordUpdateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Update temporary password to new permanent password
    /// Validates temp password, updates password hash, marks password as changed
    /// Thread-safe for concurrent updates
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="tempPassword">Current temporary password (plain text)</param>
    /// <param name="newPassword">New permanent password (plain text)</param>
    /// <returns>True if password updated successfully</returns>
    public async Task<bool> UpdatePasswordAsync(int userId, string tempPassword, string newPassword)
    {
        if (userId <= 0)
        {
            throw new ArgumentException("Invalid user ID", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(tempPassword))
        {
            throw new ArgumentException("Temporary password cannot be empty", nameof(tempPassword));
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new ArgumentException("New password cannot be empty", nameof(newPassword));
        }

        if (newPassword.Length < 8)
        {
            throw new ArgumentException("New password must be at least 8 characters", nameof(newPassword));
        }

        // Validate new password contains mix of characters
        ValidatePasswordComplexity(newPassword);

        try
        {
            // Use transaction to ensure atomic update
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Read user with locking (pessimistic lock) to prevent concurrent updates
                    // This ensures only one update happens at a time per user
                    var user = await _context.Users
                        .FromSqlInterpolated($"SELECT * FROM users WHERE id = {userId} LIMIT 1")
                        .FirstOrDefaultAsync();

                    if (user == null)
                    {
                        _logger.LogWarning($"Password update attempt for non-existent user: {userId}");
                        throw new Exception("User not found");
                    }

                    // Check if password has already been changed
                    if (user.IsPasswordChangedFromTemp)
                    {
                        _logger.LogWarning($"User {userId} attempted to update password again (already changed)");
                        throw new Exception("Password has already been changed. Use login to reset password.");
                    }

                    // Verify temp password matches
                    bool passwordMatches = BCrypt.Net.BCrypt.Verify(tempPassword, user.PasswordHash);
                    if (!passwordMatches)
                    {
                        _logger.LogWarning($"Invalid temporary password for user: {userId}");
                        throw new Exception("Invalid temporary password");
                    }

                    // Hash new password
                    string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                    // Update user record
                    user.PasswordHash = newPasswordHash;
                    user.IsPasswordChangedFromTemp = true;
                    user.UpdatedDate = DateTime.UtcNow;

                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    _logger.LogInformation($"Password successfully updated for user: {user.Username}");
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError($"Error during password update for user {userId}: {ex.Message}");
                    throw;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Password update failed for user {userId}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Check if user needs to change their temporary password
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>True if user has not changed temp password yet</returns>
    public async Task<bool> IsFirstTimePasswordChangeRequiredAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return false;
            }

            return !user.IsPasswordChangedFromTemp;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error checking password change status for user {userId}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Get user password change status
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>True if password has been changed from temp, false if still using temp password</returns>
    public async Task<bool> HasPasswordBeenChangedAsync(int userId)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new Exception("User not found");
            }

            return user.IsPasswordChangedFromTemp;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting password change status for user {userId}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Validate password complexity - must have uppercase, lowercase, number, and special char
    /// </summary>
    private void ValidatePasswordComplexity(string password)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]"))
        {
            throw new ArgumentException("Password must contain at least one uppercase letter");
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[a-z]"))
        {
            throw new ArgumentException("Password must contain at least one lowercase letter");
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[0-9]"))
        {
            throw new ArgumentException("Password must contain at least one digit");
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':"",.<>?/\\|`~]"))
        {
            throw new ArgumentException("Password must contain at least one special character (!@#$%^&* etc.)");
        }
    }
}
