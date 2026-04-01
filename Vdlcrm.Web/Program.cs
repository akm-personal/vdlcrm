using Vdlcrm.Interfaces;
using Vdlcrm.Services;
using Vdlcrm.Model;
using Vdlcrm.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using NSwag;
using NSwag.Generation.Processors.Security;
using NSwag.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
// NSwag Swagger generator for interactive UI at /swagger
builder.Services.AddSwaggerDocument(config =>
{
    config.PostProcess = document =>
    {
        document.Info.Title = "VDLCRM API";
        document.Info.Description = "API for VDLCRM Application.";
    };
    config.AddSecurity("Bearer", Enumerable.Empty<string>(), new OpenApiSecurityScheme
    {
        Type = OpenApiSecuritySchemeType.ApiKey,
        Name = "Authorization",
        In = OpenApiSecurityApiKeyLocation.Header,
        Description = "Type into the textbox: Bearer {your JWT token}."
    });
    config.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
});
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});


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
app.MapOpenApi();
app.UseOpenApi();
app.UseSwaggerUi();
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

// API Testing UI endpoint
app.MapGet("/api-docs", () =>
{
    var html = GetApiDocsHtml();
    return Results.Content(html, "text/html");
});

app.UseCors("AllowAll");

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
                const responseText = await response.text();
                if (!responseText) {
                    throw new Error('Empty response received. API endpoint /api/databasebrowser/tables might be missing or returning 404.');
                }
                if (!response.ok) {
                    throw new Error(`HTTP ${response.status}: ${responseText}`);
                }
                const data = JSON.parse(responseText);
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
                const responseText = await response.text();
                if (!response.ok) throw new Error(`HTTP ${response.status}: ${responseText}`);
                const data = JSON.parse(responseText);
                
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
                const responseText = await response.text();
                if (!response.ok) throw new Error(`HTTP ${response.status}: ${responseText}`);
                const data = JSON.parse(responseText);
                
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
                
                const responseText = await response.text();
                if (!response.ok) throw new Error(`HTTP ${response.status}: ${responseText}`);
                const data = JSON.parse(responseText);
                
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

string GetApiDocsHtml()
{
    return @"<!DOCTYPE html> 
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>VDLCRM API Documentation & Testing</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); min-height: 100vh; padding: 20px; }
        .container { max-width: 1200px; margin: 0 auto; background: white; border-radius: 10px; box-shadow: 0 10px 40px rgba(0,0,0,0.2); padding: 30px; }
        h1 { color: #333; text-align: center; margin-bottom: 10px; }
        .subtitle { text-align: center; color: #666; margin-bottom: 30px; }
        
        .api-section { margin-bottom: 40px; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden; }
        .api-header { background: #667eea; color: white; padding: 15px 20px; font-size: 18px; font-weight: bold; }
        .api-content { padding: 20px; }
        
        .endpoint { margin-bottom: 20px; padding: 15px; background: #f8f9fa; border-radius: 6px; border-left: 4px solid #667eea; }
        .method { display: inline-block; padding: 4px 8px; border-radius: 4px; font-weight: bold; font-size: 12px; margin-right: 10px; }
        .method.GET { background: #28a745; color: white; }
        .method.POST { background: #007bff; color: white; }
        .method.PUT { background: #ffc107; color: black; }
        .method.DELETE { background: #dc3545; color: white; }
        
        .endpoint-url { font-family: monospace; font-size: 14px; margin: 5px 0; }
        .endpoint-desc { color: #666; margin: 5px 0; }
        
        .test-form { margin-top: 15px; }
        .form-group { margin-bottom: 10px; }
        label { display: block; margin-bottom: 5px; font-weight: bold; }
        input, textarea { width: 100%; padding: 8px; border: 1px solid #ddd; border-radius: 4px; font-family: monospace; }
        textarea { resize: vertical; min-height: 100px; }
        
        .auth-section { background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 6px; margin-bottom: 20px; }
        .auth-token { font-family: monospace; background: #f8f9fa; padding: 5px; border-radius: 3px; }
        
        button { background: #667eea; color: white; border: none; padding: 10px 20px; border-radius: 4px; cursor: pointer; margin: 5px; }
        button:hover { background: #764ba2; }
        button.test { background: #28a745; }
        button.test:hover { background: #218838; }
        
        .response { margin-top: 15px; padding: 15px; background: #f8f9fa; border-radius: 6px; border: 1px solid #e9ecef; }
        .response pre { background: #2d3748; color: #e2e8f0; padding: 15px; border-radius: 4px; overflow-x: auto; white-space: pre-wrap; }
        
        .status { display: inline-block; padding: 2px 8px; border-radius: 12px; font-size: 12px; font-weight: bold; }
        .status.success { background: #d4edda; color: #155724; }
        .status.error { background: #f8d7da; color: #721c24; }
        
        .tabs { display: flex; gap: 10px; border-bottom: 2px solid #ddd; margin-bottom: 20px; }
        .tab { padding: 10px 20px; cursor: pointer; background: none; border: none; color: #666; }
        .tab.active { color: #667eea; border-bottom: 3px solid #667eea; }
        .tab-content { display: none; }
        .tab-content.active { display: block; }
        
        .quick-links { text-align: center; margin-top: 30px; padding: 20px; background: #f8f9fa; border-radius: 8px; }
        .quick-links a { color: #667eea; text-decoration: none; margin: 0 15px; }
        .quick-links a:hover { text-decoration: underline; }
    </style>
</head>
<body>
    <div class='container'>
        <h1>🚀 VDLCRM API Documentation & Testing</h1>
        <div class='subtitle'>Interactive API testing interface for all VDLCRM endpoints</div>
        
        <div class='auth-section'>
            <h3>🔐 Authentication</h3>
            <p>For protected endpoints, you need a JWT token. Get one by calling the login endpoint first.</p>
            <div class='form-group'>
                <label>Authorization Token (include 'Bearer ' prefix):</label>
                <input type='text' id='authToken' placeholder='Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...' />
            </div>
        </div>
        
        <div class='tabs'>
            <button class='tab active' onclick='switchTab(""auth"")'>Authentication</button>
            <button class='tab' onclick='switchTab(""students"")'>Student Management</button>
            <button class='tab' onclick='switchTab(""utility"")'>Utility</button>
        </div>
        
        <div id='auth' class='tab-content active'>
            <div class='api-section'>
                <div class='api-header'>🔐 Authentication Endpoints</div>
                <div class='api-content'>
                    
                    <div class='endpoint'>
                        <span class='method POST'>POST</span> Register User
                        <div class='endpoint-url'>/api/auth/register</div>
                        <div class='endpoint-desc'>Register a new user account</div>
                        <div class='test-form'>
                            <div class='form-group'>
                                <label>Request Body (JSON):</label>
                                <textarea id='registerBody' placeholder='Enter JSON...'>{
  ""username"": ""testuser"",
  ""email"": ""test@example.com"",
  ""password"": ""TestPass123!@#"",
  ""roleId"": 4
}</textarea>
                            </div>
                            <button class='test' onclick='testEndpoint(""POST"", ""/api/auth/register"", document.getElementById(""registerBody"").value, ""registerResult"")'>Test Register</button>
                            <div id='registerResult' class='response'></div>
                        </div>
                    </div>
                    
                    <div class='endpoint'>
                        <span class='method POST'>POST</span> Login
                        <div class='endpoint-url'>/api/auth/login</div>
                        <div class='endpoint-desc'>Authenticate and get JWT token</div>
                        <div class='test-form'>
                            <div class='form-group'>
                                <label>Request Body (JSON):</label>
                                <textarea id='loginBody' placeholder='Enter JSON...'>{
  ""username"": ""testuser"",
  ""password"": ""TestPass123!@#""
}</textarea>
                            </div>
                            <button class='test' onclick='testEndpoint(""POST"", ""/api/auth/login"", document.getElementById(""loginBody"").value, ""loginResult"")'>Test Login</button>
                            <div id='loginResult' class='response'></div>
                        </div>
                    </div>
                    
                    <div class='endpoint'>
                        <span class='method GET'>GET</span> Get Roles
                        <div class='endpoint-url'>/api/auth/roles</div>
                        <div class='endpoint-desc'>Get all available roles (requires authentication)</div>
                        <div class='test-form'>
                            <button class='test' onclick='testEndpoint(""GET"", ""/api/auth/roles"", null, ""rolesResult"")'>Test Get Roles</button>
                            <div id='rolesResult' class='response'></div>
                        </div>
                    </div>
                    
                    <div class='endpoint'>
                        <span class='method POST'>POST</span> Update Password
                        <div class='endpoint-url'>/api/auth/update-password</div>
                        <div class='endpoint-desc'>Update temporary password to permanent password</div>
                        <div class='test-form'>
                            <div class='form-group'>
                                <label>Request Body (JSON):</label>
                                <textarea id='updatePasswordBody' placeholder='Enter JSON...'>{
  ""userId"": 1,
  ""tempPassword"": ""TempPass123"",
  ""newPassword"": ""NewPass123!@#""
}</textarea>
                            </div>
                            <button class='test' onclick='testEndpoint(""POST"", ""/api/auth/update-password"", document.getElementById(""updatePasswordBody"").value, ""updatePasswordResult"")'>Test Update Password</button>
                            <div id='updatePasswordResult' class='response'></div>
                        </div>
                    </div>

                </div>
            </div>
        </div>
        
        <div id='students' class='tab-content'>
            <div class='api-section'>
                <div class='api-header'>👨‍🎓 Student Management Endpoints</div>
                <div class='api-content'>
                    
                    <div class='endpoint'>
                        <span class='method POST'>POST</span> Register Student
                        <div class='endpoint-url'>/api/student/register</div>
                        <div class='endpoint-desc'>Register a new student (auto-generates VDL ID)</div>
                        <div class='test-form'>
                            <div class='form-group'>
                                <label>Request Body (JSON):</label>
                                <textarea id='studentRegisterBody' placeholder='Enter JSON...'>{
  ""name"": ""John Doe"",
  ""email"": ""john.doe@example.com"",
  ""fatherName"": ""James Doe"",
  ""dateOfBirth"": ""2005-05-15"",
  ""gender"": ""Male"",
  ""address"": ""123 Main St"",
  ""mobileNumber"": ""9876543210"",
  ""alternateNumber"": ""9876543211"",
  ""class"": ""10A"",
  ""idProof"": ""Aadhar"",
  ""shiftType"": ""Morning"",
  ""seatNumber"": 1,
  ""studentStatus"": ""Active""
}</textarea>
                            </div>
                            <button class='test' onclick='testEndpoint(""POST"", ""/api/student/register"", document.getElementById(""studentRegisterBody"").value, ""studentRegisterResult"")'>Test Register Student</button>
                            <div id='studentRegisterResult' class='response'></div>
                        </div>
                    </div>
                    
                    <div class='endpoint'>
                        <span class='method GET'>GET</span> Get All Students
                        <div class='endpoint-url'>/api/StudentList</div>
                        <div class='endpoint-desc'>Get all registered students (Admin/Internal only)</div>
                        <div class='test-form'>
                            <button class='test' onclick='testEndpoint(""GET"", ""/api/StudentList"", null, ""studentsResult"")'>Test Get Students</button>
                            <div id='studentsResult' class='response'></div>
                        </div>
                    </div>
                    
                    <div class='endpoint'>
                        <span class='method GET'>GET</span> Get Student by ID
                        <div class='endpoint-url'>/api/student/{id}</div>
                        <div class='endpoint-desc'>Get specific student details</div>
                        <div class='test-form'>
                            <div class='form-group'>
                                <label>Student ID:</label>
                                <input type='number' id='studentId' placeholder='Enter student ID' />
                            </div>
                            <button class='test' onclick='testEndpoint(""GET"", ""/api/student/"" + document.getElementById(""studentId"").value, null, ""studentResult"")'>Test Get Student</button>
                            <div id='studentResult' class='response'></div>
                        </div>
                    </div>
                    
                </div>
            </div>
        </div>
        
        <div id='utility' class='tab-content'>
            <div class='api-section'>
                <div class='api-header'>🌤️ Utility Endpoints</div>
                <div class='api-content'>
                    
                    <div class='endpoint'>
                        <span class='method GET'>GET</span> Weather Forecast
                        <div class='endpoint-url'>/api/weatherforecast/GetWeatherForecast</div>
                        <div class='endpoint-desc'>Get sample weather forecast data</div>
                        <div class='test-form'>
                            <button class='test' onclick='testEndpoint(""GET"", ""/api/weatherforecast/GetWeatherForecast"", null, ""weatherResult"")'>Test Weather</button>
                            <div id='weatherResult' class='response'></div>
                        </div>
                    </div>
                    
                    <div class='endpoint'>
                        <span class='method GET'>GET</span> Database Tables
                        <div class='endpoint-url'>/api/databasebrowser/tables</div>
                        <div class='endpoint-desc'>Get list of all database tables</div>
                        <div class='test-form'>
                            <button class='test' onclick='testEndpoint(""GET"", ""/api/databasebrowser/tables"", null, ""tablesResult"")'>Test Get Tables</button>
                            <div id='tablesResult' class='response'></div>
                        </div>
                    </div>
                    
                </div>
            </div>
        </div>
        
        <div class='quick-links'>
            <h3>🔗 Quick Links</h3>
            <a href='/'>🏠 Home</a>
            <a href='/database'>🗄️ Database Browser</a>
            <a href='/db-viewer'>📊 Database Viewer</a>
            <a href='/openapi/v1.json' target='_blank'>📄 OpenAPI JSON</a>
            <a href='/swagger' target='_blank'>🧩 Swagger UI</a>
        </div>
    </div>

    <script>
        async function testEndpoint(method, url, body, resultId) {
            const resultDiv = document.getElementById(resultId);
            resultDiv.innerHTML = '<p>⏳ Loading...</p>';
            
            try {
                const headers = {
                    'Content-Type': 'application/json'
                };
                
                const authToken = document.getElementById('authToken').value;
                if (authToken) {
                    headers['Authorization'] = authToken;
                }
                
                const options = {
                    method: method,
                    headers: headers
                };
                
                if (body && method !== 'GET') {
                    options.body = body;
                }
                
                const response = await fetch(url, options);
                const statusClass = response.ok ? 'success' : 'error';
                const statusText = response.ok ? '✅ Success' : '❌ Error';
                
                let responseText = '';
                const contentType = response.headers.get('content-type');
                if (contentType && contentType.includes('application/json')) {
                    const jsonData = await response.json();
                    responseText = JSON.stringify(jsonData, null, 2);
                } else {
                    responseText = await response.text();
                }
                
                resultDiv.innerHTML = `
                    <div class='status ${statusClass}'>${statusText} (${response.status})</div>
                    <pre>${responseText}</pre>
                `;
            } catch (error) {
                resultDiv.innerHTML = `
                    <div class='status error'>❌ Network Error</div>
                    <pre>${error.message}</pre>
                `;
            }
        }
        
        function switchTab(tabName) {
            document.querySelectorAll('.tab-content').forEach(el => el.classList.remove('active'));
            document.querySelectorAll('.tab').forEach(el => el.classList.remove('active'));
            document.getElementById(tabName).classList.add('active');
            event.target.classList.add('active');
        }
    </script>
</body>
</html>";
}

app.Run();
