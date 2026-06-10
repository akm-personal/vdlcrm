using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vdlcrm.Model;

[Table("attendance_records")]
public class AttendanceRecord
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string VdlId { get; set; } = string.Empty;

    public int? ShiftId { get; set; } // Kis shift ke liye attendance lagi

    [Required]
    public DateTime PunchInTime { get; set; }
    
    public double? PunchInLatitude { get; set; }
    public double? PunchInLongitude { get; set; }

    public DateTime? PunchOutTime { get; set; }
    
    public double? PunchOutLatitude { get; set; }
    public double? PunchOutLongitude { get; set; }

    public bool IsAutoPunchedOut { get; set; } = false; // Agar default 8 hour wala out hua ho

    // Metadata
    public double? OvertimeMinutes { get; set; } // Punch out ke time calculate hoga

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
}