using Microsoft.AspNetCore.Http;
using Vdlcrm.Model;
using System.Linq;
using Vdlcrm.Services;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;

namespace Vdlcrm.Web;

public class TenantResolverService : ITenantResolverService
{
    public TenantInfo? CurrentTenant { get; private set; }

    public TenantResolverService(IHttpContextAccessor httpContextAccessor, MasterDbContext masterDbContext, IConfiguration configuration)
    {
        // Development Toggle: Check for the flag in appsettings.json
        // If "MultiTenancy:Enabled" is false, we stop right here.
        // This makes the whole app use the default database (vdlcrm.db).
        if (!configuration.GetValue<bool>("MultiTenancy:Enabled", true)) // Default to true if not present
        {
            CurrentTenant = null;
            return;
        }

        var context = httpContextAccessor.HttpContext;
        if (context != null)
        {
            string? resolvedTenantId = null;

            // 1. Pehle X-Tenant-Id header check karein (For public/unauthenticated requests)
            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdValues))
            {
                resolvedTenantId = tenantIdValues.FirstOrDefault();
            }

            // 2. Agar header nahi mila, aur user logged in hai, toh Token ya DB se TenantId nikalein
            if (string.IsNullOrEmpty(resolvedTenantId) && context.User?.Identity?.IsAuthenticated == true)
            {
                // JWT Token me se TenantId claim check karein
                resolvedTenantId = context.User.FindFirst("TenantId")?.Value;

                // Agar token me TenantId nahi hai, toh MasterDb.Users table se username ke basis par nikal lein
                if (string.IsNullOrEmpty(resolvedTenantId))
                {
                    var username = context.User.FindFirst(ClaimTypes.Name)?.Value;
                    if (!string.IsNullOrEmpty(username))
                    {
                        var masterUser = masterDbContext.Users.FirstOrDefault(u => u.Username == username);
                        if (masterUser != null && !string.IsNullOrEmpty(masterUser.TenantId))
                        {
                            resolvedTenantId = masterUser.TenantId;
                        }
                    }
                }
            }

            // 3. Agar TenantId successfully mil gaya, toh uski details Master DB se bind kar dein
            if (!string.IsNullOrEmpty(resolvedTenantId))
            {
                CurrentTenant = masterDbContext.Tenants.FirstOrDefault(t => t.TenantId == resolvedTenantId);
            }
        }
    }
}