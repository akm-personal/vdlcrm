using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Vdlcrm.Model.DTOs;

public class TenantCreateRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? ConnectionString { get; set; }
    public string? Provider { get; set; }
}

// JsonStringEnumConverter se enum integers ki jagah strings ("PostgreSQL", "SQLite") ke roop me dikhega
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DatabaseProvider
{
    SQLite,
    PostgreSQL,
    SqlServer
}