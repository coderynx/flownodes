using Flownodes.Shared.Interfaces;

namespace Flownodes.Shared.Services;

public class TenantService
{
    private readonly ITenantManagerGrain _tenantManager;

    public TenantService(IGrainFactory grainFactory)
    {
        _tenantManager = grainFactory.GetGrain<ITenantManagerGrain>("tenant_manager");
    }

    public async ValueTask<ITenantGrain?> CreateTenantAsync(string tenantName)
    {
        return await _tenantManager.CreateTenantAsync(tenantName);
    }

    public async ValueTask<ITenantGrain?> GetTenantAsync(string tenantName)
    {
        return await _tenantManager.GetTenantAsync(tenantName);
    }

    public async ValueTask<bool> IsTenantRegistered(string tenantName)
    {
        return await _tenantManager.IsTenantRegistered(tenantName);
    }

    public async Task RemoveTenantAsync(string tenantName)
    {
        await _tenantManager.RemoveTenantAsync(tenantName);
    }
}