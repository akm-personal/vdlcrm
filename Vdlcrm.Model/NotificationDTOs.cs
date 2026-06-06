using System;

namespace Vdlcrm.Model.DTOs;

public class NotificationCreateRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "General";
    public string TargetAudience { get; set; } = "All";
    public DateTime? ExpiryDate { get; set; }
}

public class NotificationUpdateRequest : NotificationCreateRequest
{
    public int Id { get; set; }
    // Status agar wapas Draft karna ho toh
    public string Status { get; set; } = "Draft"; 
}