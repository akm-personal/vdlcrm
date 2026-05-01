using System;

namespace Vdlcrm.Model.DTOs;

public class StudentUpdateRequest
{
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
}