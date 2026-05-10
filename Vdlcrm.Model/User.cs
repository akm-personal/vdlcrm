namespace Vdlcrm.Model;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? MobileNumber { get; set; }
    public string PasswordHash { get; set; } = string.Empty; // Will store bcrypt hash
    public int RoleId { get; set; } // Foreign key to Role.RoleId
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public string? CreatedBy { get; set; }
        public bool IsPasswordChangedFromTemp { get; set; } = false; // Tracks if temp password has been changed

    // Navigation property
    public virtual Role? Role { get; set; }
}
