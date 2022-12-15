using Flownodes.Edge.Core.Alerting;
using Flownodes.Edge.Core.Resources;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Flownodes.Edge.Node.Services;

public interface IEnvironmentService
{
    string BaseFrn { get; }
    string ServiceId { get; }
    string ClusterId { get; }
    IResourceManagerGrain GetResourceManagerGrain();
    IAlerterGrain GetAlertManagerGrain();
}

public record EnvironmentOptions
{
    public string ResourceManagerName { get; set; } = "resource_manager";
    public string AlertManagerName { get; set; } = "alert_manager";
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

    public IResourceManagerGrain GetResourceManagerGrain()
    {
        return _grainFactory.GetGrain<IResourceManagerGrain>(_environmentOptions.ResourceManagerName);
    }

    public string ServiceId => _clusterOptions.ServiceId;
    public string ClusterId => _clusterOptions.ClusterId;
    public string BaseFrn => $"frn:{ServiceId}:{ClusterId}";

    public IAlerterGrain GetAlertManagerGrain()
    {
        return _grainFactory.GetGrain<IAlerterGrain>(_environmentOptions.AlertManagerName);
    }
}