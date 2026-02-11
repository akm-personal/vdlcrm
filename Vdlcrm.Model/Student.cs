namespace Vdlcrm.Model;

public class Student
{
    public int Id { get; set; }
    public string VdlId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FatherName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string AlternateNumber { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public string IdProof { get; set; } = string.Empty;
    public string ShiftType { get; set; } = string.Empty;
    public int SeatNumber { get; set; }
    public string StudentStatus { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
