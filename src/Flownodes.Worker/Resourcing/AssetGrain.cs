using Flownodes.Sdk.Entities;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Worker.Services;
using Orleans.Runtime;

namespace Flownodes.Worker.Resourcing;

[GrainType(FlownodesEntityNames.Asset)]
internal sealed class AssetGrain : ResourceGrain, IAssetGrain
{
    public AssetGrain(ILogger<AssetGrain> logger, IEnvironmentService environmentService, IPersistentStateFactory stateFactory, IGrainContext grainContext)
        : base(logger, environmentService, null, stateFactory, grainContext)
    {
    }
}