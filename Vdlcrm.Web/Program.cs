using Vdlcrm.Interfaces;
using Vdlcrm.Services;
using Vdlcrm.Model;
using Vdlcrm.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Add database context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Add repository pattern services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Add service layer
builder.Services.AddScoped<StudentService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<RegistrationService>();  // Add RegistrationService for bulk student registration

builder.Services.AddScoped<PasswordUpdateService>();  // Add PasswordUpdateService for password updates
// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "default-secret-key-that-is-long-enough";
var issuer = jwtSettings["Issuer"] ?? "VdlcrmApi";
var audience = jwtSettings["Audience"] ?? "VdlcrmUsers";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();

// Utilities: `DateTimeHelper` is a static helper, no DI registration required.

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
// Commenting out HTTPS redirect for development purposes
// app.UseHttpsRedirection();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map root endpoint
app.MapGet("/", () => new { message = "Vdlcrm API is running", version = "1.0" });

// Database browser UI endpoint
app.MapGet("/database", async (AppDbContext db) =>
{
    var dbFile = Path.Combine(AppContext.BaseDirectory, "vdlcrm.db");
    var html = GetDatabaseBrowserHtml();
    return Results.Content(html, "text/html");
});

// Database viewer UI endpoint
app.MapGet("/db-viewer", async (AppDbContext db) =>
{
    var forecasts = await db.WeatherForecasts.ToListAsync();
    
    var html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Database Viewer</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); min-height: 100vh; padding: 20px; }
        .container { max-width: 1200px; margin: 0 auto; background: white; border-radius: 10px; box-shadow: 0 10px 40px rgba(0,0,0,0.2); padding: 30px; }
        h1 { color: #333; margin-bottom: 10px; text-align: center; }
        .info { text-align: center; color: #666; margin-bottom: 30px; }
        table { width: 100%; border-collapse: collapse; }
        th { background: #667eea; color: white; padding: 15px; text-align: left; font-weight: 600; }
        td { padding: 12px 15px; border-bottom: 1px solid #eee; }
        tr:hover { background: #f5f5f5; }
        .empty { text-align: center; padding: 40px; color: #999; }
        .actions { text-align: center; margin-top: 30px; }
        button { background: #667eea; color: white; border: none; padding: 10px 20px; border-radius: 5px; cursor: pointer; margin: 5px; }
        button:hover { background: #764ba2; }
        .api-links { margin-top: 20px; text-align: center; }
        .api-links a { color: #667eea; text-decoration: none; margin: 0 10px; }
    </style>
</head>
<body>
    <div class='container'>
        <h1>🗄️ Database Viewer</h1>
        <div class='info'>
            <p>Vdlcrm SQLite Database - WeatherForecasts Table</p>
            <p>Total Records: <strong>" + forecasts.Count + @"</strong></p>
        </div>
        
        " + (forecasts.Count == 0 ? @"
        <div class='empty'>
            <p>📭 No weather forecasts found in database</p>
            <p>Use the API to add records</p>
        </div>
        " : @"
        <table>
            <thead>
                <tr>
                    <th>ID</th>
                    <th>Date</th>
                    <th>Temperature (°C)</th>
                    <th>Temperature (°F)</th>
                    <th>Summary</th>
                </tr>
            </thead>
            <tbody>
                " + string.Join("", forecasts.Select(f => $@"
                <tr>
                    <td>{f.Id}</td>
                    <td>{f.Date:yyyy-MM-dd}</td>
                    <td>{f.TemperatureC}°C</td>
                    <td>{f.TemperatureF}°F</td>
                    <td>{f.Summary}</td>
                </tr>
                ")) + @"
            </tbody>
        </table>
        ") + @"
        
        <div class='actions'>
            <button onclick='location.reload()'>🔄 Refresh</button>
            <button onclick='window.history.back()'>← Back</button>
        </div>
        
        <div class='api-links'>
            <p>Quick Links:</p>
            <a href='/'>Home</a>
            <a href='/db-viewer'>Database Viewer</a>
            <a href='/api/weatherforecast/getweatherforecast'>API Endpoint</a>
        </div>
    </div>
</body>
</html>
    ";
    
    return Results.Content(html, "text/html");
});

app.MapControllers();

string GetDatabaseBrowserHtml()
{
    return @"<!DOCTYPE html>
<html>
<head>
    <title>Database Browser - VDLCRM</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: Arial, sans-serif; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); min-height: 100vh; padding: 20px; }
        .container { max-width: 1400px; margin: 0 auto; background: white; border-radius: 10px; box-shadow: 0 10px 40px rgba(0,0,0,0.2); padding: 30px; }
        h1 { color: #333; margin-bottom: 10px; }
        .layout { display: grid; grid-template-columns: 250px 1fr; gap: 20px; margin-top: 20px; }
        .sidebar { background: #f5f5f5; padding: 20px; border-radius: 8px; }
        .main { padding: 20px; }
        .table-list { list-style: none; }
        .table-item { padding: 10px; margin: 5px 0; background: white; border-left: 3px solid #667eea; cursor: pointer; border-radius: 4px; }
        .table-item:hover { background: #e8e8ff; }
        .table-item.active { background: #667eea; color: white; }
        .tabs { display: flex; gap: 10px; border-bottom: 2px solid #ddd; }
        .tab { padding: 10px 20px; cursor: pointer; background: none; border: none; color: #666; }
        .tab.active { color: #667eea; border-bottom: 3px solid #667eea; }
        .tab-content { display: none; margin-top: 20px; }
        .tab-content.active { display: block; }
        table { width: 100%; border-collapse: collapse; margin-top: 10px; }
        th { background: #667eea; color: white; padding: 12px; text-align: left; }
        td { padding: 10px; border-bottom: 1px solid #ddd; }
        tr:hover { background: #f0f0f0; }
        textarea { width: 100%; padding: 10px; border: 1px solid #ddd; border-radius: 4px; font-family: monospace; }
        button { background: #667eea; color: white; border: none; padding: 10px 20px; border-radius: 4px; cursor: pointer; margin-top: 10px; }
        button:hover { background: #764ba2; }
        .error { color: #d32f2f; background: #ffebee; padding: 10px; border-radius: 4px; margin: 10px 0; }
        .success { color: #388e3c; background: #e8f5e9; padding: 10px; border-radius: 4px; margin: 10px 0; }
    </style>
</head>
<body>
    <div class='container'>
        <h1>Database Browser - VDLCRM</h1>
        <div class='layout'>
            <div class='sidebar'>
                <h3>Database Tables</h3>
                <ul class='table-list' id='tableList'>
                    <li>Loading...</li>
                </ul>
            </div>
            
            <div class='main'>
                <div class='tabs'>
                    <button class='tab active' onclick='switchTab(""data"")'>View Data</button>
                    <button class='tab' onclick='switchTab(""schema"")'>Table Schema</button>
                    <button class='tab' onclick='switchTab(""query"")'>SQL Query</button>
                </div>
                
                <div id='data' class='tab-content active'>
                    <div id='dataContent'>Select a table to view data</div>
                </div>
                
                <div id='schema' class='tab-content'>
                    <div id='schemaContent'>Select a table to view schema</div>
                </div>
                
                <div id='query' class='tab-content'>
                    <h3>Execute SQL Query (SELECT only)</h3>
                    <textarea id='queryInput' rows='6' placeholder='Enter your SELECT query here...'></textarea>
                    <button onclick='executeQuery()'>Run Query</button>
                    <div id='queryResult'></div>
                </div>
            </div>
        </div>
    </div>

    <script>
        let currentTable = null;

        async function loadTables() {
            try {
                const response = await fetch('/api/databasebrowser/tables');
                const data = await response.json();
                const tableList = document.getElementById('tableList');
                tableList.innerHTML = '';
                
                data.tables.forEach(table => {
                    const li = document.createElement('li');
                    li.className = 'table-item';
                    li.textContent = table;
                    li.onclick = () => selectTable(table, li);
                    tableList.appendChild(li);
                });
            } catch (error) {
                document.getElementById('tableList').innerHTML = '<li class=""error"">Error: ' + error.message + '</li>';
            }
        }

        async function selectTable(tableName, element) {
            currentTable = tableName;
            document.querySelectorAll('.table-item').forEach(el => el.classList.remove('active'));
            element.classList.add('active');
            
            await loadTableData();
            await loadTableSchema();
        }

        async function loadTableData() {
            if (!currentTable) return;
            
            const dataContent = document.getElementById('dataContent');
            dataContent.innerHTML = 'Loading...';
            
            try {
                const response = await fetch('/api/databasebrowser/table/' + currentTable);
                const data = await response.json();
                
                if (data.rows.length === 0) {
                    dataContent.innerHTML = '<p>No records found</p>';
                    return;
                }
                
                let html = '<p>Total: ' + data.rowCount + ' records</p>';
                html += '<table><thead><tr>';
                data.columns.forEach(col => html += '<th>' + col + '</th>');
                html += '</tr></thead><tbody>';
                
                data.rows.forEach(row => {
                    html += '<tr>';
                    data.columns.forEach(col => {
                        const val = row[col];
                        html += '<td>' + (val === null ? 'NULL' : val) + '</td>';
                    });
                    html += '</tr>';
                });
                
                html += '</tbody></table>';
                dataContent.innerHTML = html;
            } catch (error) {
                dataContent.innerHTML = '<div class=""error"">Error: ' + error.message + '</div>';
            }
        }

        async function loadTableSchema() {
            if (!currentTable) return;
            
            try {
                const response = await fetch('/api/databasebrowser/schema/' + currentTable);
                const data = await response.json();
                
                let html = '<h3>Columns</h3>';
                html += data.columns.map(col => {
                    return '<div style=""padding:10px;background:#e3f2fd;margin:5px 0;border-radius:4px;""><strong>' + col.name + '</strong>: ' + col.type + (col.pk ? ' [PRIMARY KEY]' : '') + (col.notNull ? ' [NOT NULL]' : '') + '</div>';
                }).join('');
                
                document.getElementById('schemaContent').innerHTML = html;
            } catch (error) {
                document.getElementById('schemaContent').innerHTML = '<div class=""error"">Error: ' + error.message + '</div>';
            }
        }

        async function executeQuery() {
            const query = document.getElementById('queryInput').value.trim();
            if (!query) {
                document.getElementById('queryResult').innerHTML = '<div class=""error"">Please enter a query</div>';
                return;
            }
            
            const resultDiv = document.getElementById('queryResult');
            resultDiv.innerHTML = '<p>Executing...</p>';
            
            try {
                const response = await fetch('/api/databasebrowser/query', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ query: query })
                });
                
                const data = await response.json();
                
                if (!data.success) {
                    resultDiv.innerHTML = '<div class=""error"">Error: ' + data.error + '</div>';
                    return;
                }
                
                if (data.rows.length === 0) {
                    resultDiv.innerHTML = '<div class=""success"">Query executed - 0 rows</div>';
                    return;
                }
                
                let html = '<div class=""success"">Results: ' + data.rowCount + ' rows</div>';
                html += '<table><thead><tr>';
                Object.keys(data.rows[0]).forEach(col => html += '<th>' + col + '</th>');
                html += '</tr></thead><tbody>';
                
                data.rows.forEach(row => {
                    html += '<tr>';
                    Object.values(row).forEach(val => {
                        html += '<td>' + (val === null ? 'NULL' : val) + '</td>';
                    });
                    html += '</tr>';
                });
                
                html += '</tbody></table>';
                resultDiv.innerHTML = html;
            } catch (error) {
                resultDiv.innerHTML = '<div class=""error"">Error: ' + error.message + '</div>';
            }
        }

        function switchTab(tabName) {
            document.querySelectorAll('.tab-content').forEach(el => el.classList.remove('active'));
            document.querySelectorAll('.tab').forEach(el => el.classList.remove('active'));
            document.getElementById(tabName).classList.add('active');
            event.target.classList.add('active');
        }

        loadTables();
    </script>
</body>
</html>";
}

app.Run();
