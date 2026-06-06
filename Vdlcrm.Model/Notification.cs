using System;

namespace Vdlcrm.Model;

public class Notification
{
    public int Id { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    public string Message { get; set; } = string.Empty;
    
    public string Type { get; set; } = "General"; // General, Exam, Fee, Urgent, Event
    
    public string TargetAudience { get; set; } = "All"; // Kisko bhejna hai (e.g. All, Role_4)
    
    public string Status { get; set; } = "Draft"; // Draft, Live, Archived
    
    public DateTime? PublishDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}