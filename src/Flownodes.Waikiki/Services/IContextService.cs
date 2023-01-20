using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;

namespace Flownodes.Waikiki.Services;

public interface IContextService
{
    ITenantGrain? TenantGrain { get; }
    IResourceManagerGrain? ResourceManager { get; }
    IAlertManagerGrain? AlertManager { get; }
    IList<ResourceSummary>? ResourceSummaries { get; }
    Task SetTenantAsync(string tenantName);
    Task UpdateResourceSummaries();
}