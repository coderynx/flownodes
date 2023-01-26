using Flownodes.Shared.Interfaces;
using Flownodes.Worker.Services;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

[StatelessWorker(1)]
public class ClusterGrain : Grain, IClusterGrain
{
    private readonly IEnvironmentService _environmentService;
    private readonly ILogger<ClusterGrain> _logger;
    private readonly IClusterManifestProvider _manifest;

    public ClusterGrain(IEnvironmentService environmentService, ILogger<ClusterGrain> logger, IClusterManifestProvider manifest)
    {
        _environmentService = environmentService;
        _logger = logger;
        _manifest = manifest;
    }

    public ValueTask<ClusterInformation> GetClusterInformation()
    {
        _logger.LogDebug("Requested cluster information");
        return ValueTask.FromResult(
            new ClusterInformation(_environmentService.ClusterId, _environmentService.ServiceId, _manifest.Current.Silos.Count, _manifest.Current.Version.ToString()));
    }
}