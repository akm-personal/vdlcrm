using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;
using Vdlcrm.Services;
using Vdlcrm.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Vdlcrm.Web.Middleware;

public class DynamicPermissionMiddleware
{
    private readonly RequestDelegate _next;

    public DynamicPermissionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await _next(context);
            return;
        }

        // Check if endpoint allows anonymous access (public)
        var hasAllowAnonymous = endpoint.Metadata.OfType<IAllowAnonymous>().Any();
        if (hasAllowAnonymous)
        {
            await _next(context);
            return;
        }

        if (endpoint is RouteEndpoint routeEndpoint)
        {
            var routeUrl = routeEndpoint.RoutePattern.RawText;
            var httpMethod = context.Request.Method;

            if (routeUrl != null && routeUrl.StartsWith("api/"))
            {
                var userRoleClaim = context.User.FindFirst("RoleId")?.Value;
                if (string.IsNullOrEmpty(userRoleClaim) || !int.TryParse(userRoleClaim, out int roleId))
                {
                    await _next(context); // Code-based attributes will handle invalid users
                    return;
                }

                using var scope = context.RequestServices.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // DB mein check karein ki is URL ke liye koi permission maujud hai ya nahi
                var isRouteConfigured = await dbContext.Set<EndpointPermission>()
                    .AnyAsync(p => p.RouteUrl == routeUrl && p.HttpMethod == httpMethod);

                if (isRouteConfigured) // Agar DB me config hai, tabhi dynamic check lagoo hoga
                {
                    var hasAccess = await dbContext.Set<EndpointPermission>()
                        .AnyAsync(p => p.RouteUrl == routeUrl && p.HttpMethod == httpMethod && p.RoleId == roleId);

                    if (!hasAccess)
                    {
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync("{\"message\": \"Access Denied: You do not have dynamic permission for this API.\"}");
                        return;
                    }
                }
            }
        }
        await _next(context);
    }
}