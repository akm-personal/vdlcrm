using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vdlcrm.Services;

namespace Vdlcrm.Web.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class DatabaseBrowserController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public DatabaseBrowserController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Get all table names from the database
    /// </summary>
    [HttpGet("tables")]
    public async Task<ActionResult<object>> GetTables()
    {
        try
        {
            var connection = _dbContext.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
                using (var result = await command.ExecuteReaderAsync())
                {
                    var tables = new List<string>();
                    while (await result.ReadAsync())
                    {
                        tables.Add(result.GetString(0));
                    }
                    return Ok(new { tables });
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get data from a specific table
    /// </summary>
    [HttpGet("table/{tableName}")]
    public async Task<ActionResult<object>> GetTableData(string tableName, [FromQuery] int limit = 100)
    {
        try
        {
            // Validate table name to prevent SQL injection
            if (!IsValidTableName(tableName))
                return BadRequest(new { error = "Invalid table name" });

            var connection = _dbContext.Database.GetDbConnection();
            await connection.OpenAsync();

            // Get column information
            var columns = new List<string>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"PRAGMA table_info({tableName});";
                using (var result = await command.ExecuteReaderAsync())
                {
                    while (await result.ReadAsync())
                    {
                        columns.Add(result.GetString(1)); // column name
                    }
                }
            }

            // Get data
            var rows = new List<Dictionary<string, object?>>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT * FROM [{tableName}] LIMIT {limit};";
                using (var result = await command.ExecuteReaderAsync())
                {
                    while (await result.ReadAsync())
                    {
                        var row = new Dictionary<string, object?>();
                        for (int i = 0; i < columns.Count; i++)
                        {
                            row[columns[i]] = result.IsDBNull(i) ? null : result.GetValue(i);
                        }
                        rows.Add(row);
                    }
                }
            }

            return Ok(new
            {
                tableName,
                columns,
                rowCount = rows.Count,
                rows
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = ex.Message });
        }
    }

    /// <summary>
    /// Execute a custom SQL query
    /// </summary>
    [HttpPost("query")]
    public async Task<ActionResult<object>> ExecuteQuery([FromBody] QueryRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                return BadRequest(new { error = "Query cannot be empty" });

            var connection = _dbContext.Database.GetDbConnection();
            await connection.OpenAsync();

            var query = request.Query.Trim();
            bool isSelect = query.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) || 
                            query.StartsWith("PRAGMA", StringComparison.OrdinalIgnoreCase);

            var rows = new List<Dictionary<string, object?>>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = query;
                
                if (isSelect)
                {
                    using (var result = await command.ExecuteReaderAsync())
                    {
                        var columns = new List<string>();
                        for (int i = 0; i < result.FieldCount; i++)
                        {
                            columns.Add(result.GetName(i));
                        }

                        while (await result.ReadAsync())
                        {
                            var row = new Dictionary<string, object?>();
                            for (int i = 0; i < columns.Count; i++)
                            {
                                row[columns[i]] = result.IsDBNull(i) ? null : result.GetValue(i);
                            }
                            rows.Add(row);
                        }

                        return Ok(new
                        {
                            success = true,
                            rowCount = rows.Count,
                            rows
                        });
                    }
                }
                else
                {
                    var rowsAffected = await command.ExecuteNonQueryAsync();
                    return Ok(new { success = true, rowCount = rowsAffected, message = $"Query executed successfully. {rowsAffected} row(s) affected." });
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = ex.Message });
        }
    }

    /// <summary>
    /// Truncate table and reset auto-increment identity
    /// </summary>
    [HttpPost("truncate/{tableName}")]
    public async Task<ActionResult<object>> TruncateTable(string tableName)
    {
        try
        {
            if (!IsValidTableName(tableName))
                return BadRequest(new { error = "Invalid table name" });

            var connection = _dbContext.Database.GetDbConnection();
            await connection.OpenAsync();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"DELETE FROM [{tableName}];";
                await command.ExecuteNonQueryAsync();
                
                command.CommandText = $"DELETE FROM sqlite_sequence WHERE name='{tableName}';";
                try { await command.ExecuteNonQueryAsync(); } catch { /* Ignore if sqlite_sequence doesn't exist */ }
            }

            return Ok(new { success = true, message = $"Table {tableName} truncated successfully and indexing reset to 1." });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get table schema information
    /// </summary>
    [HttpGet("schema/{tableName}")]
    public async Task<ActionResult<object>> GetTableSchema(string tableName)
    {
        try
        {
            if (!IsValidTableName(tableName))
                return BadRequest(new { error = "Invalid table name" });

            var connection = _dbContext.Database.GetDbConnection();
            await connection.OpenAsync();

            var columns = new List<dynamic>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"PRAGMA table_info({tableName});";
                using (var result = await command.ExecuteReaderAsync())
                {
                    while (await result.ReadAsync())
                    {
                        columns.Add(new
                        {
                            id = result.GetInt32(0),
                            name = result.GetString(1),
                            type = result.GetString(2),
                            notNull = result.GetInt32(3) == 1,
                            defaultValue = result.IsDBNull(4) ? null : result.GetValue(4),
                            pk = result.GetInt32(5) == 1
                        });
                    }
                }
            }

            return Ok(new { tableName, columns });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { error = ex.Message });
        }
    }

    private bool IsValidTableName(string tableName)
    {
        // Only allow alphanumeric characters and underscores
        return !string.IsNullOrWhiteSpace(tableName) &&
               System.Text.RegularExpressions.Regex.IsMatch(tableName, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
    }

    public class QueryRequest
    {
        public string Query { get; set; } = string.Empty;
    }
}
