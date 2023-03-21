using Flownodes.Sdk.Entities;
using Flownodes.Shared.Alerting.Grains;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Shared.Tenanting.Grains;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Runtime;

namespace Flownodes.Worker.Services;

public interface IEnvironmentService
{
    string ServiceId { get; }
    string ClusterId { get; }
    int SilosCount { get; }

    ITenantManagerGrain GetTenantManager();
    Task<IResourceManagerGrain?> GetResourceManager(string tenantName);
    Task<IAlertManagerGrain?> GetAlertManager(string tenantName);
}

public class EnvironmentService : IEnvironmentService
{
    private readonly IClusterManifestProvider _clusterManifestProvider;
    private readonly ClusterOptions _clusterOptions;
    private readonly ITenantManagerGrain _tenantManager;

    public EnvironmentService(IOptions<ClusterOptions> clusterOptions, IGrainFactory grainFactory,
        IClusterManifestProvider clusterManifestProvider)
    {
        _clusterOptions = clusterOptions.Value;

        var id = new FlownodesId(FlownodesEntity.TenantManager, FlownodesEntityNames.TenantManager);
        _tenantManager = grainFactory.GetGrain<ITenantManagerGrain>(id);

        _clusterManifestProvider = clusterManifestProvider;
    }

    public string ServiceId => _clusterOptions.ServiceId;
    public string ClusterId => _clusterOptions.ClusterId;
    public int SilosCount => _clusterManifestProvider.Current.Silos.Count;

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