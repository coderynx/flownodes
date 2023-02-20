using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;

namespace Flownodes.Waikiki.Services;

public class ContextService : IContextService
{
    private readonly ILogger<ContextService> _logger;
    private readonly ITenantManagerGrain _tenantManagerGrain;

    public ContextService(IGrainFactory grainFactory, ILogger<ContextService> logger)
    {
        _logger = logger;
        _tenantManagerGrain = grainFactory.GetGrain<ITenantManagerGrain>("tenant_manager");
        ResourceManager = grainFactory.GetGrain<IResourceManagerGrain>("resource_manager");

        ClusterGrain = grainFactory.GetGrain<IClusterGrain>(0);
    }

    public string TenantName { get; private set; }
    public ITenantGrain? TenantGrain { get; private set; }
    public IClusterGrain? ClusterGrain { get; }
    public IResourceManagerGrain? ResourceManager { get; }
    public IAlertManagerGrain? AlertManager { get; private set; }
    public IList<Resource>? ResourceSummaries { get; private set; }

    public async Task SetTenantAsync(string tenantName)
    {
        TenantName = tenantName;

        TenantGrain = await _tenantManagerGrain.GetTenantAsync(tenantName);
        if (TenantGrain is null) throw new Exception($"Tenant {tenantName} not found");

        await UpdateResourceSummaries();

        _logger.LogInformation("Switched tenant to: {TenantName}", tenantName);
    }

    public async Task UpdateResourceSummaries()
    {
        ResourceSummaries = await ResourceManager.GetAllResourceSummaries(TenantName);
    }
}