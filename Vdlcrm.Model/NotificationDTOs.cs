using System;
using System.ComponentModel.DataAnnotations;

namespace Vdlcrm.Model.DTOs;

public class NotificationCreateRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;
    [Required]
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info";
    public string TargetAudience { get; set; } = "All";
    public DateTime? ExpiryDate { get; set; }
}

public class NotificationUpdateRequest : NotificationCreateRequest
{
    [Required]
    public string Status { get; set; } = "Draft";
}