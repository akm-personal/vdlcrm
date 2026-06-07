using System;

namespace Vdlcrm.Model;

public class Shift
{
    public int Id { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public int? Status { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public bool? IsDeleted { get; set; }
}