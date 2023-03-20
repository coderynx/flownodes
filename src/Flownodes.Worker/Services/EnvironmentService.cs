using Flownodes.Sdk;
using Flownodes.Shared.Alerting;
using Flownodes.Shared.Cluster;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Tenanting;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Flownodes.Worker.Services;

public interface IEnvironmentService
{
    string ServiceId { get; }
    string ClusterId { get; }
    ITenantManagerGrain GetTenantManagerGrain();
    IResourceManagerGrain GetResourceManagerGrain();
    IAlertManagerGrain GetAlertManagerGrain();
    IClusterGrain GetClusterGrain();
}

public class EnvironmentService : IEnvironmentService
{
    private readonly ClusterOptions _clusterOptions;
    private readonly IGrainFactory _grainFactory;

    public EnvironmentService(IGrainFactory grainFactory, IOptions<ClusterOptions> clusterOptions)
    {
        _grainFactory = grainFactory;
        _clusterOptions = clusterOptions.Value;
    }

    public ITenantManagerGrain GetTenantManagerGrain()
    {
        return _grainFactory.GetGrain<ITenantManagerGrain>(FlownodesEntityNames.TenantManager);
    }

    public IResourceManagerGrain GetResourceManagerGrain()
    {
        return _grainFactory.GetGrain<IResourceManagerGrain>(FlownodesEntityNames.ResourceManager);
    }

    public string ServiceId => _clusterOptions.ServiceId;
    public string ClusterId => _clusterOptions.ClusterId;

    public IAlertManagerGrain GetAlertManagerGrain()
    {
        return _grainFactory.GetGrain<IAlertManagerGrain>(FlownodesEntityNames.AlertManager);
    }

    public IClusterGrain GetClusterGrain()
    {
        return _grainFactory.GetGrain<IClusterGrain>(0);
    }
}