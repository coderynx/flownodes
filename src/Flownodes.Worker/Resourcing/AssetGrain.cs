using Flownodes.Sdk.Entities;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Resourcing.Grains;
using Orleans.Runtime;

namespace Flownodes.Worker.Resourcing;

[GrainType(FlownodesEntityNames.Asset)]
internal sealed class AssetGrain : ResourceGrain, IAssetGrain
{
    public AssetGrain(ILogger<AssetGrain> logger, IPersistentStateFactory stateFactory, IGrainContext grainContext)
        : base(logger, stateFactory, grainContext)
    {
    }

    public async ValueTask<BaseResourceSummary> GetSummary()
    {
        return new AssetSummary(Id, Metadata, await GetState());
    }
}