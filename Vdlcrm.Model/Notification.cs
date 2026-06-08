using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vdlcrm.Model;

[Table("notifications")]
public class Notification
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Type { get; set; } = string.Empty; // Urgent, Info, Warning

    [MaxLength(100)]
    public string TargetAudience { get; set; } = string.Empty; // All, Staff, Students

    [MaxLength(50)]
    public string Status { get; set; } = "Draft"; // Draft, Live, Archived

    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }
    public DateTime? PublishDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
}