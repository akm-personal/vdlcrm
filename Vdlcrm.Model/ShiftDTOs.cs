using System;
using System.Collections.Generic;

namespace Vdlcrm.Model.DTOs;

public class CreateShiftRequest
{
    public string ShiftName { get; set; } = string.Empty;
    public int? Status { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class CreateShiftResponse : Shift
{
    public string Message { get; set; } = string.Empty;
}

public class UpdateShiftRequest
{
    public string? ShiftName { get; set; }
    public int? Status { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class UpdateShiftResponse : Shift
{
    public string Message { get; set; } = string.Empty;
}

public class DeleteShiftResponse
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime DeletedDate { get; set; }
}

public class GetShiftsResponse
{
    public List<Shift> Shifts { get; set; } = new List<Shift>();
    public int Count { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class GetShiftByIdResponse : Shift
{
    public string Message { get; set; } = string.Empty;
}