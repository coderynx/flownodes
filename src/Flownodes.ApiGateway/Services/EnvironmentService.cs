using Flownodes.Core.Interfaces;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Flownodes.ApiGateway.Services;

public interface IEnvironmentService
{
    string BaseFrn { get; }
    string ServiceId { get; }
    string ClusterId { get; }
    IResourceManagerGrain GetResourceManagerGrain();
    IAlertManagerGrain GetAlertManagerGrain();
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

    public IAlertManagerGrain GetAlertManagerGrain()
    {
        return _grainFactory.GetGrain<IAlertManagerGrain>(_environmentOptions.AlertManagerName);
    }
}