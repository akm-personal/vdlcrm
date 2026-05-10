using System;
using System.Collections.Generic;

namespace Vdlcrm.Model;

public class Seat
{
    public int Id { get; set; }
    public int SeatRowId { get; set; }
    public string SeatLabel { get; set; } = string.Empty;
    public int SeatOrder { get; set; }
    public bool IsLocked { get; set; }
    public bool IsDeleted { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedDate { get; set; }

    public SeatRow? SeatRow { get; set; }
    public ICollection<SeatAssignment> SeatAssignments { get; set; } = new List<SeatAssignment>();
}
