using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Vdlcrm.Model.DTOs;

// JsonStringEnumConverter se enum integers ki jagah strings ("PostgreSQL", "SQLite") ke roop me dikhega
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DatabaseProvider
{
    SQLite,
    PostgreSQL,
    SqlServer
}