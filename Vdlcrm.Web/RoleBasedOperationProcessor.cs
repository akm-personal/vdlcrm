using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using NSwag.Generation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System;
using System.Collections.Generic;

namespace Vdlcrm.Web.Swagger;

public class RoleBasedOperationProcessor : IOperationProcessor
{
    private readonly string _targetRole;

    public RoleBasedOperationProcessor(string targetRole)
    {
        _targetRole = targetRole;
    }

    public bool Process(OperationProcessorContext context)
    {
        bool hasAllowAnonymous = false;
        List<IAuthorizeData> authorizeData = new List<IAuthorizeData>();

        // Modern ASP.NET Core approach: Read attributes directly from EndpointMetadata (100% accurate)
        if (context is AspNetCoreOperationProcessorContext aspNetContext)
        {
            var metadata = aspNetContext.ApiDescription?.ActionDescriptor?.EndpointMetadata;
            if (metadata != null)
            {
                hasAllowAnonymous = metadata.OfType<IAllowAnonymous>().Any();
                authorizeData = metadata.OfType<IAuthorizeData>().ToList();
            }
        }
        else
        {
            // Fallback for older contexts
            var methodInfo = context.MethodInfo;
            if (methodInfo != null)
            {
                hasAllowAnonymous = methodInfo.GetCustomAttributes(true).OfType<IAllowAnonymous>().Any() ||
                                    (methodInfo.DeclaringType?.GetCustomAttributes(true).OfType<IAllowAnonymous>().Any() ?? false);
                
                var declaringTypeAuthData = methodInfo.DeclaringType?.GetCustomAttributes(true).OfType<IAuthorizeData>() ?? Enumerable.Empty<IAuthorizeData>();
                authorizeData = methodInfo.GetCustomAttributes(true).OfType<IAuthorizeData>()
                    .Concat(declaringTypeAuthData)
                    .ToList();
            }
        }

        if (hasAllowAnonymous) return true; // Show public endpoints to everyone

        if (!authorizeData.Any())
        {
            // No [Authorize] attribute at all -> public endpoint
            return true;
        }

        // Gather roles defined like [Authorize(Roles = "Admin,Internal User")]
        var requiredRoles = authorizeData
            .Where(a => !string.IsNullOrWhiteSpace(a.Roles))
            .SelectMany(a => a.Roles!.Split(',').Select(r => r.Trim()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (requiredRoles.Count == 0)
        {
            // [Authorize] without specific roles -> any logged in user can access, so show it
            return true;
        }

        // If endpoint has specific role requirements, show it ONLY if target role is in the list
        return requiredRoles.Contains(_targetRole, StringComparer.OrdinalIgnoreCase);
    }
}