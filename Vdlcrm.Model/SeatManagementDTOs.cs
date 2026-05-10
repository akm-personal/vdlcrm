using System.Collections.Generic;

namespace Vdlcrm.Model.DTOs;

public class CreateSeatRowRequest
{
    public string? RowName { get; set; }
}

public class UpdateSeatRowRequest
{
    public string? RowName { get; set; }
    public bool? IsLocked { get; set; }
}

public class CreateSeatRequest
{
    public int SeatRowId { get; set; }
    public string? SeatLabel { get; set; }
}

public class UpdateSeatRequest
{
    public string? SeatLabel { get; set; }
    public bool? IsLocked { get; set; }
}

public class CreateSeatAssignmentRequest
{
    public int SeatId { get; set; }
    public int ShiftId { get; set; }
    public int StudentId { get; set; }
}

public class SeatRowResponse
{
    public int Id { get; set; }
    public string RowName { get; set; } = string.Empty;
    public int RowOrder { get; set; }
    public bool IsLocked { get; set; }
    public bool IsDeleted { get; set; }
    public List<SeatResponse> Seats { get; set; } = new List<SeatResponse>();
}

public class SeatResponse
{
    public int Id { get; set; }
    public int SeatRowId { get; set; }
    public string SeatLabel { get; set; } = string.Empty;
    public int SeatOrder { get; set; }
    public bool IsLocked { get; set; }
    public bool IsDeleted { get; set; }
    public List<SeatAssignmentResponse> Assignments { get; set; } = new List<SeatAssignmentResponse>();
}

public class SeatAssignmentResponse
{
    public int Id { get; set; }
    public int SeatId { get; set; }
    public int ShiftId { get; set; }
    public int StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? StudentVdlId { get; set; }
    public string? ShiftName { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime AssignedDate { get; set; }
}
