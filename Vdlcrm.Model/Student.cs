using System.ComponentModel.DataAnnotations.Schema;

namespace Vdlcrm.Model;

public class Student
{
    public int Id { get; set; }
    public string VdlId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FatherName { get; set; }
    public string? Gender { get; set; }
    public int? SeatNumber { get; set; }
    public string? ShiftType { get; set; }
    public string? Address { get; set; }
    public string? AlternateNumber { get; set; }
    public string? Class { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? IdProof { get; set; }
    public string? MobileNumber { get; set; }
    public string? StudentStatus { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public string? CreatedBy { get; set; }

    [NotMapped]
    public string? LastFeeStatus { get; set; }

    [NotMapped]
    public DateTime? LastFeeStartDate { get; set; }

    [NotMapped]
    public DateTime? LastFeeEndDate { get; set; }

    [NotMapped]
    public int? LastFeeRecordId { get; set; }

    [NotMapped]
    public decimal? RemainingBalance { get; set; }

    [NotMapped]
    public int? RoleId { get; set; }

    [NotMapped]
    public string? RoleName { get; set; }
}
