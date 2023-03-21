using Flownodes.Sdk;
using Flownodes.Shared.Alerting;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Tenanting;

namespace Flownodes.Worker.Services;

public class ManagersService : IManagersService
{
    private readonly ITenantManagerGrain _tenantManager;

    public ManagersService(IGrainFactory grainFactory)
    {
        var id = new FlownodesId(FlownodesEntity.TenantManager, FlownodesEntityNames.TenantManager);
        _tenantManager = grainFactory.GetGrain<ITenantManagerGrain>(id);
    }

    public ITenantManagerGrain GetTenantManager()
    {
        return _tenantManager;
    }

    public async Task<IResourceManagerGrain?> GetResourceManager(string tenantName)
    {
        var tenant = await _tenantManager.GetTenantAsync(tenantName);
        if (tenant is null) return null;

        return await tenant.GetResourceManager();
    }

    public async Task<IAlertManagerGrain?> GetAlertManager(string tenantName)
    {
        var tenant = await _tenantManager.GetTenantAsync(tenantName);
        if (tenant is null) return null;

        return await tenant.GetAlertManager();
    }
}