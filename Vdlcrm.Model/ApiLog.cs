using System;

namespace Vdlcrm.Model;

public class ApiLog
{
    public string? UserId { get; set; } // Kisne hit kiya (VDL ID)
    public string Method { get; set; } = string.Empty; // GET, POST, etc.
    public string Path { get; set; } = string.Empty; // API endpoint URL
    public string? QueryString { get; set; }
    public string? RequestBody { get; set; }
    public int StatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public long ExecutionTimeMs { get; set; } // Request complete hone me kitna time laga
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}