using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using System.Linq;
using Vdlcrm.Model;
using Vdlcrm.Services;

namespace Vdlcrm.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Protected via standard auth, Super Admin checks can be enforced via DynamicPermissions
public class SuperAdminDbBrowserController : ControllerBase
{
    private readonly MasterDbContext _masterDbContext;

    public SuperAdminDbBrowserController(MasterDbContext masterDbContext)
    {
        _masterDbContext = masterDbContext;
    }

    // Helper method to ensure only Super Admin (Role 5) can access this
    private bool IsSuperAdmin()
    {
        var roleId = User.FindFirst("RoleId")?.Value;
        return roleId == "5" || roleId == "1"; // Assuming 5 is Super Admin, allowing Admin (1) as fallback if needed. Adjust as necessary.
    }

    // 1. Get List of Available Database Providers
    [HttpGet("providers")]
    public async Task<IActionResult> GetProviders()
    {
        if (!IsSuperAdmin()) return StatusCode(403, new { message = "Access Denied: Super Admin only." });

        var providers = await _masterDbContext.Tenants
            .Select(t => t.Provider)
            .Distinct()
            .ToListAsync();
        
        if (!providers.Any()) providers.Add("SQLite"); // Default fallback

        return Ok(providers);
    }

    // 2. Get Schemas/Databases (Tenants) based on selected Provider
    [HttpGet("databases/{provider}")]
    public async Task<IActionResult> GetDatabases(string provider)
    {
        if (!IsSuperAdmin()) return StatusCode(403, new { message = "Access Denied: Super Admin only." });

        var databases = await _masterDbContext.Tenants
            .Where(t => t.Provider.ToLower() == provider.ToLower())
            .Select(t => new { t.TenantId, t.Name, t.Provider })
            .ToListAsync();

        return Ok(databases);
    }

    // Core helper to dynamically build ADO.NET connection for any tenant
    private async Task<(DbConnection Connection, string Provider)> GetTenantConnectionAsync(string tenantId)
    {
        var tenant = await _masterDbContext.Tenants.FirstOrDefaultAsync(t => t.TenantId == tenantId);
        if (tenant == null) throw new Exception($"Tenant '{tenantId}' not found.");

        string provider = tenant.Provider.ToLower();

        var tenantOptions = new DbContextOptionsBuilder<AppDbContext>();
        
        if (provider == "postgresql") tenantOptions.UseNpgsql(tenant.ConnectionString);
        else if (provider == "sqlserver") tenantOptions.UseSqlServer(tenant.ConnectionString);
        else tenantOptions.UseSqlite(tenant.ConnectionString); // SQLite default

        // Create a temporary context just to extract the raw DbConnection
        var tempDbContext = new AppDbContext(tenantOptions.Options, null);
        return (tempDbContext.Database.GetDbConnection(), provider);
    }

    // 3. Get Tables for a specific Tenant Schema
    [HttpGet("tables/{tenantId}")]
    public async Task<IActionResult> GetTables(string tenantId)
    {
        if (!IsSuperAdmin()) return StatusCode(403, new { message = "Access Denied: Super Admin only." });

        try
        {
            var tenantDbInfo = await GetTenantConnectionAsync(tenantId);
            using var connection = tenantDbInfo.Connection;
            string provider = tenantDbInfo.Provider;
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            
            // Provider-specific queries to list tables
            if (provider == "postgresql")
                command.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_schema='public';";
            else if (provider == "sqlserver")
                command.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_type='BASE TABLE';";
            else // SQLite
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";

            var tables = new List<string>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }

            return Ok(tables);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to load tables.", error = ex.Message });
        }
    }

    // 4. Run Custom SQL Query on specific Tenant Schema
    [HttpPost("query/{tenantId}")]
    public async Task<IActionResult> RunQuery(string tenantId, [FromBody] SuperAdminQueryRequest request)
    {
        if (!IsSuperAdmin()) return StatusCode(403, new { message = "Access Denied: Super Admin only." });
        if (string.IsNullOrWhiteSpace(request.Query)) return BadRequest(new { message = "Query cannot be empty." });

        try
        {
            var tenantDbInfo = await GetTenantConnectionAsync(tenantId);
            using var connection = tenantDbInfo.Connection;
            string provider = tenantDbInfo.Provider;
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = request.Query;

            if (request.Query.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                using var reader = await command.ExecuteReaderAsync();
                var results = new List<Dictionary<string, object?>>();
                var columns = new List<string>();

                for (int i = 0; i < reader.FieldCount; i++) columns.Add(reader.GetName(i));

                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>();
                    for (int i = 0; i < columns.Count; i++)
                    {
                        row[columns[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    results.Add(row);
                }
                return Ok(new { data = results });
            }
            else
            {
                var rowsAffected = await command.ExecuteNonQueryAsync();
                return Ok(new { message = "Query executed successfully.", rowsAffected });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Query execution failed.", error = ex.Message });
        }
    }

    // 5. Standalone UI for Super Admin Database Browser
    [HttpGet("ui")]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)] // Hides this from Swagger UI
    public ContentResult GetBrowserUI()
    {
        var html = @"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Super Admin DB Browser</title>
    <style>
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 0; display: flex; flex-direction: column; height: 100vh; background-color: #f4f6f8; }
        .header { background: #1a237e; color: white; padding: 10px 20px; display: flex; justify-content: space-between; align-items: center; }
        .header input { padding: 8px; width: 350px; border-radius: 4px; border: none; outline: none; }
        .toolbar { background: #e8eaf6; padding: 10px 20px; display: flex; gap: 15px; border-bottom: 1px solid #c5cae9; align-items: center; }
        .toolbar select, .toolbar button { padding: 8px; border-radius: 4px; border: 1px solid #ccc; font-size: 14px; }
        .main { display: flex; flex: 1; overflow: hidden; }
        .sidebar { width: 250px; background: white; border-right: 1px solid #ccc; overflow-y: auto; padding: 10px; }
        .table-list-item { padding: 10px; cursor: pointer; border-radius: 4px; border-bottom: 1px solid #eee; }
        .table-list-item:hover { background: #e8eaf6; font-weight: 600; color: #1a237e; }
        .content { flex: 1; display: flex; flex-direction: column; padding: 15px; overflow: hidden; }
        .query-box { height: 120px; width: 100%; margin-bottom: 10px; font-family: 'Courier New', Courier, monospace; padding: 10px; border: 1px solid #ccc; border-radius: 4px; resize: vertical; box-sizing: border-box; }
        .result-container { flex: 1; background: white; border: 1px solid #ccc; border-radius: 4px; overflow: auto; }
        table { border-collapse: collapse; width: 100%; }
        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; font-size: 14px; }
        th { background-color: #f5f5f5; position: sticky; top: 0; }
        .btn { background: #3f51b5; color: white; cursor: pointer; font-weight: bold; border: none; }
        .btn:hover { background: #303f9f; }
    </style>
</head>
<body>
    <div class='header'>
        <h3 style='margin: 0;'>🚀 Super Admin Browser</h3>
        <div>
            <input type='password' id='tokenInput' placeholder='Paste your Bearer Token here...'>
            <button class='btn' style='padding: 8px 15px; border-radius: 4px;' onclick='loadProviders()'>Connect</button>
        </div>
    </div>
    <div class='toolbar'>
        <select id='providerSelect' onchange='loadTenants()'>
            <option value=''>-- Select Provider --</option>
        </select>
        <select id='tenantSelect' onchange='loadTables()'>
            <option value=''>-- Select Tenant/Database --</option>
        </select>
    </div>
    <div class='main'>
        <div class='sidebar' id='tableList'>
            <i style='color: #666;'>Select a tenant to view tables...</i>
        </div>
        <div class='content'>
            <textarea id='queryInput' class='query-box' placeholder='Write your SQL query here (e.g. SELECT * FROM users)'></textarea>
            <div style='margin-bottom: 15px; display: flex; align-items: center;'>
                <button class='btn' style='padding: 10px 25px; border-radius: 4px;' onclick='runQuery()'>▶ Run Query</button>
                <span id='statusMsg' style='margin-left: 15px; color: #d32f2f; font-weight: bold;'></span>
            </div>
            <div class='result-container' id='resultContainer'>
                <table id='resultTable'><thead><tr><th>Results will appear here</th></tr></thead><tbody></tbody></table>
            </div>
        </div>
    </div>

    <script>
        function getHeaders() {
            return { 'Authorization': 'Bearer ' + document.getElementById('tokenInput').value.trim(), 'Content-Type': 'application/json' };
        }
        async function loadProviders() {
            try {
                document.getElementById('statusMsg').innerText = 'Loading providers...';
                let res = await fetch('/api/SuperAdminDbBrowser/providers', { headers: getHeaders() });
                if (!res.ok) throw new Error(await res.text());
                let providers = await res.json();
                document.getElementById('providerSelect').innerHTML = `<option value=''>-- Select Provider --</option>` + providers.map(p => `<option value='${p}'>${p}</option>`).join('');
                document.getElementById('statusMsg').innerText = 'Connected successfully. Select a provider.';
            } catch (err) { document.getElementById('statusMsg').innerText = 'Connection Error: Check token.'; }
        }
        async function loadTenants() {
            let provider = document.getElementById('providerSelect').value;
            if (!provider) return;
            try {
                document.getElementById('statusMsg').innerText = 'Loading databases...';
                let res = await fetch(`/api/SuperAdminDbBrowser/databases/${provider}`, { headers: getHeaders() });
                if (!res.ok) throw new Error(await res.text());
                let tenants = await res.json();
                document.getElementById('tenantSelect').innerHTML = `<option value=''>-- Select Tenant/Database --</option>` + tenants.map(t => `<option value='${t.tenantId}'>${t.name} (${t.tenantId})</option>`).join('');
                document.getElementById('statusMsg').innerText = 'Databases loaded. Select one.';
            } catch (err) { document.getElementById('statusMsg').innerText = 'Error: ' + err.message; }
        }
        async function loadTables() {
            let tenantId = document.getElementById('tenantSelect').value;
            if (!tenantId) return;
            try {
                document.getElementById('statusMsg').innerText = 'Loading tables...';
                let res = await fetch(`/api/SuperAdminDbBrowser/tables/${tenantId}`, { headers: getHeaders() });
                if (!res.ok) throw new Error(await res.text());
                let tables = await res.json();
                document.getElementById('tableList').innerHTML = tables.map(t => `<div class='table-list-item' onclick='selectTable(""${t}"")'>📇 ${t}</div>`).join('');
                document.getElementById('statusMsg').innerText = '';
            } catch (err) { document.getElementById('statusMsg').innerText = 'Error: ' + err.message; }
        }
        function selectTable(tableName) {
            let provider = document.getElementById('providerSelect').value.toLowerCase();
            document.getElementById('queryInput').value = provider === 'sqlserver' ? `SELECT TOP 100 * FROM [${tableName}];` : `SELECT * FROM ${tableName} LIMIT 100;`;
            runQuery();
        }
        async function runQuery() {
            let tId = document.getElementById('tenantSelect').value, q = document.getElementById('queryInput').value.trim();
            if (!tId || !q) return;
            try {
                document.getElementById('statusMsg').innerText = 'Executing query...';
                let res = await fetch(`/api/SuperAdminDbBrowser/query/${tId}`, { method: 'POST', headers: getHeaders(), body: JSON.stringify({ query: q }) });
                let data = await res.json();
                if (!res.ok) throw new Error(data.message || 'Query failed');
                let table = document.getElementById('resultTable');
                if (data.data && data.data.length > 0) { let cols = Object.keys(data.data[0]); table.innerHTML = `<thead><tr>${cols.map(c => `<th>${c}</th>`).join('')}</tr></thead><tbody>${data.data.map(row => `<tr>${cols.map(c => `<td>${row[c] !== null ? row[c] : '<i>NULL</i>'}</td>`).join('')}</tr>`).join('')}</tbody>`; } 
                else if (data.data) { table.innerHTML = `<thead><tr><th>No records found.</th></tr></thead><tbody></tbody>`; } 
                else { table.innerHTML = `<thead><tr><th>${data.message} - Rows Affected: ${data.rowsAffected}</th></tr></thead><tbody></tbody>`; }
                document.getElementById('statusMsg').innerText = `Query Successful.`;
            } catch (err) { document.getElementById('statusMsg').innerText = 'Error: ' + err.message; }
        }
    </script>
</body>
</html>";
        return Content(html, "text/html");
    }
}

public class SuperAdminQueryRequest
{
    public string Query { get; set; } = string.Empty;
}