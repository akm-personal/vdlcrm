using Microsoft.AspNetCore.Http;
using Vdlcrm.Model;
using System.Linq;
using Vdlcrm.Services;

namespace Vdlcrm.Web;

public class TenantResolverService : ITenantResolverService
{
    public TenantInfo? CurrentTenant { get; private set; }

    public TenantResolverService(IHttpContextAccessor httpContextAccessor, MasterDbContext masterDbContext)
    {
        var context = httpContextAccessor.HttpContext;
        if (context != null)
        {
            // Header se TenantId nikal rahe hain. (e.g. X-Tenant-Id: vdl_school_1)
            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdValues))
            {
                var tenantId = tenantIdValues.FirstOrDefault();
                if (!string.IsNullOrEmpty(tenantId))
                {
                    // Master DB se us tenant ki connection details nikal lo
                    CurrentTenant = masterDbContext.Tenants.FirstOrDefault(t => t.TenantId == tenantId);
                }
            }
        }
    }
}