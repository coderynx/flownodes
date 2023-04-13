using Flownodes.Sdk.Entities;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Resourcing.Grains;
using Orleans.Runtime;

namespace Flownodes.Worker.Resourcing;

[GrainType(FlownodesEntityNames.Asset)]
internal sealed class AssetGrain : ResourceGrain, IAssetGrain
{
    private readonly ILogger<AssetGrain> _logger;
    private readonly IJournaledStoreGrain<Dictionary<string, object?>> _properties;

    public AssetGrain(ILogger<AssetGrain> logger, IPersistentStateFactory stateFactory, IGrainContext grainContext)
        : base(logger, stateFactory, grainContext)
    {
        _logger = logger;
        _properties = GrainFactory.GetGrain<IJournaledStoreGrain<Dictionary<string, object?>>>($"{Id}_properties");
    }

    public async ValueTask<ResourceSummary> GetSummary()
    {
        return new ResourceSummary(Id, Metadata.State, await GetProperties());
    }

    public async ValueTask<Dictionary<string, object?>> GetProperties()
    {
        _logger.LogDebug("Retrieved properties of AssetGrain {@AssetGrainId}", Id);
        return await _properties.Get();
    }

    public async Task UpdatePropertiesAsync(Dictionary<string, object?> properties)
    {
        await _properties.UpdateAsync(properties);
        _logger.LogInformation("Updated AssetGrain {@AssetGrainId} properties", Id);
    }

    public async Task ClearPropertiesAsync()
    {
        await _properties.ClearAsync();
        _logger.LogInformation("Cleared AssetGrain {@AssetGrainId} properties", Id);
    }
}