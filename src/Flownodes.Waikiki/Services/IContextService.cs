using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;

namespace Flownodes.Waikiki.Services;

public interface IContextService
{
    ITenantGrain? TenantGrain { get; }
    IClusterGrain? ClusterGrain { get; }
    IResourceManagerGrain? ResourceManager { get; }
    IAlertManagerGrain? AlertManager { get; }
    IList<Resource>? ResourceSummaries { get; }
    Task SetTenantAsync(string tenantName);
    Task UpdateResourceSummaries();
}