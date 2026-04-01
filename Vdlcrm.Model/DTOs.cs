namespace Vdlcrm.Model.DTOs;

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
    public UserDto? User { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? MobileNumber { get; set; }
    public string Password { get; set; } = string.Empty;
    public int RoleId { get; set; }
}

/// <summary>
/// Student registration response DTO
/// Contains auto-generated VDL ID and temporary password
/// </summary>
public class StudentRegistrationResponse
{
    public int StudentId { get; set; }
    public string VdlId { get; set; } = string.Empty;  // Auto-generated StuID (VDL001, VDL002, etc.)
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Username { get; set; }  // Auto-generated username
    public string? TempPassword { get; set; }  // Temporary password for initial login
    public int RoleId { get; set; } = 4;  // Always 4 for student
    public string Message { get; set; } = "Student registered successfully";
    public DateTime CreatedDate { get; set; }
}

/// <summary>
/// Password update request DTO - for changing temporary password to permanent
/// </summary>
public class UpdatePasswordRequest
{
    public int UserId { get; set; }
    public string TempPassword { get; set; } = string.Empty;  // Current temporary password
    public string NewPassword { get; set; } = string.Empty;  // New permanent password
}

/// <summary>
/// Password update response DTO
/// </summary>
public class UpdatePasswordResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string? Username { get; set; }
    public bool IsPasswordChanged { get; set; }
    public DateTime UpdatedDate { get; set; }
}
