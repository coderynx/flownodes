using Flownodes.Shared.Interfaces;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Flownodes.Worker.Services;

public interface IEnvironmentService
{
    string BaseFrn { get; }
    string ServiceId { get; }
    string ClusterId { get; }
    ITenantManagerGrain GetTenantManagerGrain();
    IResourceManagerGrain GetResourceManagerGrain();
    IAlertManagerGrain GetAlertManagerGrain();
    IClusterGrain GetClusterGrain();
}

public record EnvironmentOptions
{
    public string ResourceManagerName { get; set; } = "resource_manager";
    public string AlertManagerName { get; set; } = "alert_manager";
    public string TenantManagerName { get; set; } = "tenant_manager";
}

public class EnvironmentService : IEnvironmentService
{
    private readonly ClusterOptions _clusterOptions;
    private readonly EnvironmentOptions _environmentOptions;
    private readonly IGrainFactory _grainFactory;

    public EnvironmentService(IOptions<EnvironmentOptions> configuration, IGrainFactory grainFactory,
        IOptions<ClusterOptions> clusterOptions)
    {
        _environmentOptions = configuration.Value;
        _grainFactory = grainFactory;
        _clusterOptions = clusterOptions.Value;
    }

    public ITenantManagerGrain GetTenantManagerGrain()
    {
        return _grainFactory.GetGrain<ITenantManagerGrain>(_environmentOptions.TenantManagerName);
    }

    public IResourceManagerGrain GetResourceManagerGrain()
    {
        return _grainFactory.GetGrain<IResourceManagerGrain>(_environmentOptions.ResourceManagerName);
    }

    public string ServiceId => _clusterOptions.ServiceId;
    public string ClusterId => _clusterOptions.ClusterId;
    public string BaseFrn => $"frn:{ServiceId}:{ClusterId}";

    public IAlertManagerGrain GetAlertManagerGrain()
    {
        return _grainFactory.GetGrain<IAlertManagerGrain>(_environmentOptions.AlertManagerName);
    }

    public IClusterGrain GetClusterGrain()
    {
        return _grainFactory.GetGrain<IClusterGrain>(0);
    }
}