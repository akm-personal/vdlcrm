using System;

namespace Vdlcrm.Model;

public class SeatAssignment
{
    public int Id { get; set; }
    public int SeatId { get; set; }
    public int ShiftId { get; set; }
    public int StudentId { get; set; }
    public string? CreatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime AssignedDate { get; set; }
    public DateTime? RemovedDate { get; set; }

    public Seat? Seat { get; set; }
    public Shift? Shift { get; set; }
    public Student? Student { get; set; }
}
