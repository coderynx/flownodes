using Flownodes.Shared.Interfaces;

namespace Flownodes.Shared.Services;

public class TenantService
{
    public TenantService(IGrainFactory grainFactory)
    {
        _tenantManager = grainFactory.GetGrain<ITenantManagerGrain>("tenant_manager");
    }

    private readonly ITenantManagerGrain _tenantManager;
    
    public async ValueTask<ITenantGrain?> CreateTenantAsync(string tenantName)
    {
        return await _tenantManager.CreateTenantAsync(tenantName);
    }

    public async ValueTask<ITenantGrain?> GetTenantAsync(string tenantName)
    {
        return await _tenantManager.GetTenantAsync(tenantName);
    }
}