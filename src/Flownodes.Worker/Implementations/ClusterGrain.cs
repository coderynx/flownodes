using Flownodes.Core.Interfaces;
using Flownodes.Worker.Services;
using Orleans.Concurrency;

namespace Flownodes.Worker.Implementations;

[StatelessWorker(1)]
public class ClusterGrain : Grain, IClusterGrain
{
    private readonly IEnvironmentService _environmentService;
    private readonly ILogger<ClusterGrain> _logger;

    public ClusterGrain(IEnvironmentService environmentService, ILogger<ClusterGrain> logger)
    {
        _environmentService = environmentService;
        _logger = logger;
    }

    public ValueTask<ClusterInformation> GetClusterInformation()
    {
        _logger.LogDebug("Requested cluster information");

        return ValueTask.FromResult(
            new ClusterInformation(_environmentService.ClusterId, _environmentService.ServiceId));
    }
}