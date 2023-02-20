using Flownodes.Shared.Interfaces;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

public class TenantGrain : Grain, ITenantGrain
{
    private readonly ILogger<TenantGrain> _logger;
    private readonly IPersistentState<Dictionary<string, string?>> _metadata;

    public TenantGrain(ILogger<TenantGrain> logger,
        [PersistentState("tenantMetadata")] IPersistentState<Dictionary<string, string?>> metadata)
    {
        _logger = logger;
        _metadata = metadata;
    }

    private string Name => this.GetPrimaryKeyString();

    public async Task UpdateMetadataAsync(Dictionary<string, string?> metadata)
    {
        _metadata.State = metadata;
        await _metadata.WriteStateAsync();

        _logger.LogInformation("Updated metadata of tenant {TenantName}", Name);
    }

    public ValueTask<Dictionary<string, string?>> GetMetadataAsync()
    {
        _logger.LogDebug("Retrieving metadata of tenant {TenantName}", Name);
        return ValueTask.FromResult(_metadata.State);
    }

    public async Task ClearMetadataAsync()
    {
        await _metadata.ClearStateAsync();
        _logger.LogInformation("Cleared metadata of tenant {TenantName}", Name);
    }

    public ValueTask<Dictionary<string, string?>> GetMetadata()
    {
        _logger.LogDebug("Retrieved configuration of tenant {TenantName}", Name);
        return ValueTask.FromResult(_metadata.State);
    }
}