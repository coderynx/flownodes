using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;
using Orleans.Runtime;
using OrleansCodeGen.Orleans;

namespace Flownodes.Worker.Implementations;

public class TenantGrain : Grain, ITenantGrain
{
    private readonly IPersistentState<Dictionary<string, string?>> _metadata;
    private readonly ILogger<TenantGrain> _logger;

    public TenantGrain(ILogger<TenantGrain> logger,
        [PersistentState("tenantMetadata")]
        IPersistentState<Dictionary<string, string?>> metadata)
    {
        _logger = logger;
        _metadata = metadata;
    }

    private string Name => this.GetPrimaryKeyString();

    public async Task UpdateMetadataAsync(Dictionary<string, string?> metadata)
    {
        _metadata.State = metadata;
        await _metadata.WriteStateAsync();

        _logger.LogInformation("Updated metadata for tenant {TenantName}", Name);
    }

    public async Task ClearMetadataAsync()
    {
        await _metadata.ClearStateAsync();
        _logger.LogInformation("Cleared metadata og tenant {TenantName}", Name);
    }

    public ValueTask<Dictionary<string, string?>> GetMetadata()
    {
        _logger.LogDebug("Retrieved configuration for tenant {TenantName}", Name);
        return ValueTask.FromResult(_metadata.State);
    }
}