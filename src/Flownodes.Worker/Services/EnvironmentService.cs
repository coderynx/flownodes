using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Flownodes.Worker.Services;

public interface IEnvironmentService
{
    string ServiceId { get; }
    string ClusterId { get; }
}

public class EnvironmentService : IEnvironmentService
{
    private readonly ClusterOptions _clusterOptions;

    public EnvironmentService(IOptions<ClusterOptions> clusterOptions)
    {
        _clusterOptions = clusterOptions.Value;
    }

    public string ServiceId => _clusterOptions.ServiceId;
    public string ClusterId => _clusterOptions.ClusterId;
}