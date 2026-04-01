using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Vdlcrm.Model.DTOs;
using Vdlcrm.Services;

namespace Vdlcrm.Web.Controllers.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
        private readonly PasswordUpdateService _passwordUpdateService;
    private readonly ILogger<AuthController> _logger;

        public AuthController(AuthService authService, PasswordUpdateService passwordUpdateService, ILogger<AuthController> logger)
    {
        _authService = authService;
            _passwordUpdateService = passwordUpdateService;
        _logger = logger;
    }

    /// <summary>
    /// Login with username and password
    /// </summary>
    /// <param name="request">Login request containing username and password</param>
    /// <returns>JWT token and user information if login is successful</returns>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation($"Login attempt for user: {request.Username}");

        var response = await _authService.LoginAsync(request);

        if (!response.Success)
        {
            _logger.LogWarning($"Failed login attempt for user: {request.Username}");
            return Unauthorized(response);
        }

        _logger.LogInformation($"Successful login for user: {request.Username}");
        return Ok(response);
    }

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <param name="request">Registration request</param>
    /// <returns>Registration response</returns>
    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation($"Registration attempt for user: {request.Username}");

        var response = await _authService.RegisterAsync(request);

        if (!response.Success)
        {
            _logger.LogWarning($"Failed registration attempt for user: {request.Username}");
            return BadRequest(response);
        }

        _logger.LogInformation($"Successful registration for user: {request.Username}");
        return Ok(response);
    }

    /// <summary>
    /// Get all available roles
    /// </summary>
    /// <returns>List of all roles</returns>
    [HttpGet("roles")]
    public async Task<ActionResult> GetRoles()
    {
        try
        {
            var roles = await _authService.GetAllRolesAsync();
            return Ok(new { success = true, data = roles });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching roles: {ex.Message}");
            return StatusCode(500, new { success = false, message = "Error fetching roles" });
        }
    }

    /// <summary>
    /// Update temporary password to permanent password - First-time login users only
    /// Thread-safe for concurrent password updates
    /// </summary>
    /// <param name="request">Password update request with user ID, temp password, and new password</param>
    /// <returns>Password update response with success status</returns>
    [HttpPost("update-password")]
    public async Task<ActionResult<UpdatePasswordResponse>> UpdatePassword([FromBody] UpdatePasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new UpdatePasswordResponse
            {
                Success = false,
                Message = "Invalid request data",
                UserId = request.UserId
            });
        }

        if (request.UserId <= 0)
        {
            return BadRequest(new UpdatePasswordResponse
            {
                Success = false,
                Message = "Invalid user ID"
            });
        }

        try
        {
            _logger.LogInformation($"Password update attempt for user ID: {request.UserId}");

            // Check if password change is required (first-time user)
            var isFirstTimeChange = await _passwordUpdateService.IsFirstTimePasswordChangeRequiredAsync(request.UserId);
            if (!isFirstTimeChange)
            {
                _logger.LogWarning($"User {request.UserId} attempted to change password but already changed");
                return BadRequest(new UpdatePasswordResponse
                {
                    Success = false,
                    Message = "Password has already been changed. Please use forgot password to reset.",
                    UserId = request.UserId,
                    IsPasswordChanged = true
                });
            }

            // Update password (thread-safe)
            bool success = await _passwordUpdateService.UpdatePasswordAsync(
                request.UserId,
                request.TempPassword,
                request.NewPassword
            );

            _logger.LogInformation($"Password successfully updated for user ID: {request.UserId}");

            return Ok(new UpdatePasswordResponse
            {
                Success = true,
                Message = "Password updated successfully. Please login with your new password.",
                UserId = request.UserId,
                IsPasswordChanged = true,
                UpdatedDate = DateTime.UtcNow
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning($"Validation error during password update for user {request.UserId}: {ex.Message}");
            return BadRequest(new UpdatePasswordResponse
            {
                Success = false,
                Message = ex.Message,
                UserId = request.UserId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating password for user {request.UserId}: {ex.Message}");
            return StatusCode(500, new UpdatePasswordResponse
            {
                Success = false,
                Message = ex.Message.Contains("Invalid temporary password") ? "Invalid temporary password" :
                         ex.Message.Contains("User not found") ? "User not found" :
                         "An error occurred while updating password",
                UserId = request.UserId
            });
        }
    }

    /// <summary>
    /// Logout user (requires authentication token)
    /// </summary>
    /// <returns>Logout success message</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(200)]
    public ActionResult<object> Logout()
    {
        // Extract user information from the JWT Token claims
        var username = User.Identity?.Name ?? "Unknown User";
        var userId = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        _logger.LogInformation($"Logout endpoint called by User ID: {userId}, Username: {username}");
        
        // Note: Since this API uses stateless JWTs, the actual logout happens on the client side
        // by deleting the token from localStorage, sessionStorage, or cookies.
        // To enforce server-side logout in the future, you would implement a Token Blacklist here.
        return Ok(new { success = true, message = $"User '{username}' logged out successfully. Please remove the token from the client application." });
    }
}
