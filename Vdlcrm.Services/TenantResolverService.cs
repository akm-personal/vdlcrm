using Vdlcrm.Model;

namespace Vdlcrm.Services;

public interface ITenantResolverService
{
    TenantInfo? CurrentTenant { get; }
}