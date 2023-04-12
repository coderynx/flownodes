using Flownodes.Sdk.Entities;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Resourcing.Grains;
using Orleans.Runtime;

namespace Flownodes.Worker.Resourcing;

[GrainType(FlownodesEntityNames.Asset)]
internal sealed class AssetGrain : ResourceGrain, IAssetGrain
{
    private readonly ILogger<AssetGrain> _logger;

    private readonly IJournaledStoreGrain<Dictionary<string, object?>> _state;

    public AssetGrain(ILogger<AssetGrain> logger, IPersistentStateFactory stateFactory, IGrainContext grainContext)
        : base(logger, stateFactory, grainContext)
    {
        _logger = logger;
        _state = GrainFactory.GetGrain<IJournaledStoreGrain<Dictionary<string, object?>>>($"{Id}_state");
    }

    public async ValueTask<BaseResourceSummary> GetSummary()
    {
        return new AssetSummary(Id, Metadata.State, await GetState());
    }

    public async ValueTask<Dictionary<string, object?>> GetState()
    {
        _logger.LogInformation("Retrieved state of AssetGrain {@AssetGrainId}", Id);
        return await _state.Get();
    }

    public async Task UpdateStateAsync(Dictionary<string, object?> state)
    {
        await _state.UpdateAsync(state);
        _logger.LogInformation("Updated AssetGrain {@AssetGrainId} state", Id);
    }

    public async Task ClearStateAsync()
    {
        await _state.ClearAsync();
        _logger.LogInformation("Cleared AssetGrain {@AssetGrainId} state", Id);
    }
}