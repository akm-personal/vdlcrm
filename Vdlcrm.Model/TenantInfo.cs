using System.ComponentModel.DataAnnotations;

namespace Vdlcrm.Model;

public class TenantInfo
{
    [Key]
    public string TenantId { get; set; } = string.Empty; // Example: "vdl_school_1"
    public string Name { get; set; } = string.Empty;
    
    // Yahan hum connection string aur database type store karenge
    public string ConnectionString { get; set; } = string.Empty;
    public string Provider { get; set; } = "SQLite"; // "SQLite", "PostgreSQL", "SqlServer"
}