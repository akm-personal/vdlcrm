namespace Vdlcrm.Model;

public class Role
{
    public int RoleSequenceId { get; set; } // Auto-generated primary key
    public string RoleName { get; set; } = string.Empty; // admin, Internal User, External User, student
    public int RoleId { get; set; } // 1, 2, 3, 4
    public string? CreatedBy { get; set; }
}
