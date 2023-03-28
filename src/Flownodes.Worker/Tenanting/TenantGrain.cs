using Flownodes.Sdk.Entities;
using Flownodes.Shared.Alerting.Grains;
using Flownodes.Shared.Eventing;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Shared.Tenanting.Grains;
using Orleans.Runtime;

namespace Flownodes.Worker.Tenanting;

[GrainType(FlownodesEntityNames.Tenant)]
public class TenantGrain : Grain, ITenantGrain
{
    private readonly ILogger<TenantGrain> _logger;
    private readonly IPersistentState<Dictionary<string, string?>> _metadataStore;

    public TenantGrain(ILogger<TenantGrain> logger,
        [PersistentState("tenantMetadataStore")]
        IPersistentState<Dictionary<string, string?>> metadataStore)
    {
        _logger = logger;
        _metadataStore = metadataStore;
    }

    private Dictionary<string, string?> Metadata
    {
        get => _metadataStore.State;
        set => _metadataStore.State = value;
    }

    private FlownodesId Id => (FlownodesId)this.GetPrimaryKeyString();

    public async Task UpdateMetadataAsync(Dictionary<string, string?> metadata)
    {
        Metadata = metadata;
        await _metadataStore.WriteStateAsync();

        _logger.LogInformation("Updated metadata of tenant {@TenantId}", Id);
    }

    public async Task ClearMetadataAsync()
    {
        await _metadataStore.ClearStateAsync();
        _logger.LogInformation("Cleared metadata of tenant {@TenantId}", Id);
    }

    public ValueTask<Dictionary<string, string?>> GetMetadata()
    {
        _logger.LogDebug("Retrieved metadata of tenant {@TenantId}", Id);
        return ValueTask.FromResult(Metadata);
    }

    public ValueTask<IResourceManagerGrain> GetResourceManager()
    {
        var id = new FlownodesId(FlownodesEntity.ResourceManager, Id.FirstName);
        return ValueTask.FromResult(GrainFactory.GetGrain<IResourceManagerGrain>(id));
    }

    public ValueTask<IAlertManagerGrain> GetAlertManager()
    {
        var id = new FlownodesId(FlownodesEntity.AlertManager, Id.FirstName);
        return ValueTask.FromResult(GrainFactory.GetGrain<IAlertManagerGrain>(id));
    }

    public ValueTask<IEventBookGrain> GetEventBook()
    {
        var id = new FlownodesId(FlownodesEntity.EventBook, Id.FirstName);
        return ValueTask.FromResult(GrainFactory.GetGrain<IEventBookGrain>(id));
    }

    public ValueTask<FlownodesId> GetId()
    {
        return ValueTask.FromResult(Id);
    }
}