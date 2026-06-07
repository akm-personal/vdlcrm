using System;
using System.Collections.Generic;

namespace Vdlcrm.Model;

public class SeatRow
{
    public int Id { get; set; }
    public string RowName { get; set; } = string.Empty;
    public int RowOrder { get; set; }
    public bool IsLocked { get; set; }
    public bool IsDeleted { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime UpdatedDate { get; set; }

    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
