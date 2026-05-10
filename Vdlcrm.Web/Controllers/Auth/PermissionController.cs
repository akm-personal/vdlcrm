using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using Vdlcrm.Services;
using Vdlcrm.Model;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Security.Claims;

namespace Vdlcrm.Web.Controllers.Auth;

[ApiController]
[Route("api/[controller]")]
// [Authorize(Roles = "Admin")] // Sirf Admin hi permission dekh/change kar paye
public class PermissionController : ControllerBase
{
    private readonly EndpointDataSource _endpointDataSource;
    private readonly AppDbContext _dbContext;

    // EndpointDataSource ASP.NET Core ka in-built feature hai jo sare routes track karta hai
    public PermissionController(EndpointDataSource endpointDataSource, AppDbContext dbContext)
    {
        _endpointDataSource = endpointDataSource;
        _dbContext = dbContext;
    }

    [HttpGet("all-endpoints")]
    public IActionResult GetAllEndpoints()
    {
        // Memory se sare API routes nikalna
        var endpoints = _endpointDataSource.Endpoints.OfType<RouteEndpoint>();
        
        var apiEndpoints = endpoints
            .Where(e => e.RoutePattern.RawText != null && e.RoutePattern.RawText.StartsWith("api/"))
            .Select(e => new 
            {
                RouteUrl = e.RoutePattern.RawText,
                HttpMethod = e.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault()?.HttpMethods.FirstOrDefault() ?? "ANY"
            })
            .ToList();

        return Ok(new { count = apiEndpoints.Count, endpoints = apiEndpoints });
    }

    [HttpGet("db-permissions")]
    public async Task<IActionResult> GetDbPermissions()
    {
        // DB me stored sari permissions get karein
        var permissions = await _dbContext.Set<EndpointPermission>().ToListAsync();
        
        // APIs ko group karein taki response me comma-separated array (like [1, 2]) dikhe
        var groupedPermissions = permissions
            .GroupBy(p => new { p.RouteUrl, p.HttpMethod })
            .Select(g => new 
            {
                routeUrl = g.Key.RouteUrl,
                httpMethod = g.Key.HttpMethod,
                roleIds = g.Select(p => p.RoleId).OrderBy(r => r).ToList()
            });

        return Ok(groupedPermissions);
    }

    [HttpPost("save-permission")]
    public async Task<IActionResult> SavePermission([FromBody] EndpointPermissionRequest request)
    {
        // Pehle purani permissions delete karein (agar exists karti hain us route aur method ke liye)
        var existingPermissions = await _dbContext.Set<EndpointPermission>()
            .Where(p => p.RouteUrl == request.RouteUrl && p.HttpMethod == request.HttpMethod)
            .ToListAsync();

        _dbContext.Set<EndpointPermission>().RemoveRange(existingPermissions);

        // Nayi permissions add karein
        if (request.RoleIds != null && request.RoleIds.Any())
        {
            // Token se current admin user ka VDLID nikalein
            var currentVdlId = User.FindFirst(ClaimTypes.Name)?.Value ?? "system";

            var newPermissions = request.RoleIds.Select(roleId => new EndpointPermission
            {
                RouteUrl = request.RouteUrl,
                HttpMethod = request.HttpMethod,
                RoleId = roleId,
                CreatedBy = currentVdlId
            });

            await _dbContext.Set<EndpointPermission>().AddRangeAsync(newPermissions);
        }

        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Permissions dynamically updated successfully!" });
    }

    [HttpPost("seed-default")]
    public async Task<IActionResult> SeedDefaultPermissions()
    {
        // Yahan humne standard roles define kar diye hain sari APIs ke liye
        var defaultPermissions = new List<(string Route, string Method, int[] Roles)>
        {
            // Database Browser & Permissions (Strictly Admin = 1)
            ("api/DatabaseBrowser/tables", "GET", new[] { 1 }),
            ("api/DatabaseBrowser/table/{tableName}", "GET", new[] { 1 }),
            ("api/DatabaseBrowser/query", "POST", new[] { 1 }),
            ("api/DatabaseBrowser/truncate/{tableName}", "POST", new[] { 1 }),
            ("api/DatabaseBrowser/schema/{tableName}", "GET", new[] { 1 }),
            ("api/Permission/all-endpoints", "GET", new[] { 1 }),
            ("api/Permission/db-permissions", "GET", new[] { 1 }),
            ("api/Permission/save-permission", "POST", new[] { 1 }),
            
            // Shifts Management (Admin=1, Internal=2)
            ("api/Shifts/create", "POST", new[] { 1 }),
            ("api/Shifts/update/{id}", "PUT", new[] { 1 }),
            ("api/Shifts/delete/{id}", "DELETE", new[] { 1 }),
            ("api/Shifts/all", "GET", new[] { 1, 2 }),
            ("api/Shifts/{id}", "GET", new[] { 1, 2 }),
            
            // Fee Management (Admin=1, Internal=2, Student=4 for read-only)
            ("api/Fee/record", "POST", new[] { 1, 2 }),
            ("api/Fee/payment", "POST", new[] { 1, 2 }),
            ("api/Fee/student/{vdlId}/balance", "GET", new[] { 1, 2, 4 }),
            ("api/Fee/student/{vdlId}/records", "GET", new[] { 1, 2, 4 }),
            
            // Auth & Profiles (All Logged-in Users)
            ("api/Auth/roles", "GET", new[] { 1, 2 }),
            ("api/Auth/update-password", "POST", new[] { 1, 2, 3, 4 }),
            ("api/Auth/logout", "POST", new[] { 1, 2, 3, 4 }),
            
            // Student Management
            ("api/Student/list", "GET", new[] { 1, 2 }),
            ("api/Student/details/{vdlId}", "GET", new[] { 1, 2, 3, 4 }),
            ("api/Student/update/{vdlId}", "PUT", new[] { 1 }),
            ("api/Student/delete/{vdlId}", "DELETE", new[] { 1 }),
            
            // Utilities
            ("api/WeatherForecast/GetWeatherForecast", "GET", new[] { 1, 2, 3, 4 })
        };

        // Pehle se majood permissions ko memory me layen (delete nahi karna hai)
        var existingPermissions = await _dbContext.Set<EndpointPermission>().ToListAsync();

        // Expected list tayar karein
        var expectedPermissions = defaultPermissions.SelectMany(dp => 
            dp.Roles.Select(role => new EndpointPermission { RouteUrl = dp.Route, HttpMethod = dp.Method, RoleId = role, CreatedBy = "system" })
        ).ToList();

        var newPermissionsToInsert = new List<EndpointPermission>();

        foreach (var ep in expectedPermissions)
        {
            // Check karein ki kya ye same route, method aur role ki permission pehle se database me hai?
            bool exists = existingPermissions.Any(p => p.RouteUrl == ep.RouteUrl && p.HttpMethod == ep.HttpMethod && p.RoleId == ep.RoleId);
            if (!exists)
            {
                newPermissionsToInsert.Add(ep);
            }
        }

        // Sirf jo nayi permissions hain, unhe hi insert karein
        if (newPermissionsToInsert.Any())
        {
            await _dbContext.Set<EndpointPermission>().AddRangeAsync(newPermissionsToInsert);
            await _dbContext.SaveChangesAsync();
        }

        return Ok(new { 
            message = "Default permissions synchronized successfully!", 
            newEntriesAdded = newPermissionsToInsert.Count,
            totalEntriesInDb = existingPermissions.Count + newPermissionsToInsert.Count
        });
    }
}

// Request payload model
public class EndpointPermissionRequest
{
    public string RouteUrl { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public int[] RoleIds { get; set; } = System.Array.Empty<int>();
}