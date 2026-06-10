using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Vdlcrm.Model;

[Table("student_documents")]
public class StudentDocument
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string VdlId { get; set; } = string.Empty; // Student mapping

    [Required]
    [MaxLength(50)]
    public string DocumentType { get; set; } = string.Empty; // e.g., "Aadhar", "PAN", "Photo"

    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty; // User ko dikhane ke liye

    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty; // Server par actual path (e.g., "/uploads/docs/VDL001_Aadhar.pdf")

    [Required]
    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty; // MIME type (e.g., "application/pdf")

    public long FileSizeBytes { get; set; }

    public bool IsVerified { get; set; } = false; // Admin approval ke liye

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string UploadedBy { get; set; } = string.Empty;
}