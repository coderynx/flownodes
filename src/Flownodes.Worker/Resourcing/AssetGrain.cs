using Flownodes.Sdk.Entities;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Worker.Services;

namespace Flownodes.Worker.Resourcing;

[GrainType(FlownodesEntityNames.Asset)]
internal sealed class AssetGrain : ResourceGrain, IAssetGrain
{
    public AssetGrain(ILogger<AssetGrain> logger, IEnvironmentService environmentService)
        : base(logger, environmentService, null)
    {
    }
}