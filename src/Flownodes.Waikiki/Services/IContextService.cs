using Flownodes.Shared.Interfaces;

namespace Flownodes.Waikiki.Services;

public interface IContextService
{
    ITenantGrain? TenantGrain { get; }
    Task SetTenantAsync(string tenantName);

    ValueTask<IResourceManagerGrain?> GetResourceManagerAsync();
}