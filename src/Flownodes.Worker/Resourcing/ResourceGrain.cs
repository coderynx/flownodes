using Flownodes.Sdk.Entities;
using Flownodes.Shared.Alerting.Grains;
using Flownodes.Shared.Eventing;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Worker.Extensions;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Flownodes.Worker.Resourcing;

[Reentrant]
internal abstract class ResourceGrain : Grain
{
    private readonly ILogger<ResourceGrain> _logger;
    protected readonly IPersistentState<Dictionary<string, object?>> Metadata;

    protected ResourceGrain(ILogger<ResourceGrain> logger, IPersistentStateFactory stateFactory,
        IGrainContext grainContext)
    {
        _logger = logger;
        Metadata = stateFactory.Create<Dictionary<string, object?>>(grainContext,
            new PersistentStateAttribute("resourceMetadataStore"));
    }

    protected FlownodesId Id => (FlownodesId)this.GetPrimaryKeyString();
    protected string TenantName => Id.FirstName;
    private FlownodesId ResourceManagerId => new(FlownodesEntity.ResourceManager, TenantName);
    protected IResourceManagerGrain ResourceManager => GrainFactory.GetGrain<IResourceManagerGrain>(ResourceManagerId);
    private FlownodesId AlertManagerId => new(FlownodesEntity.AlertManager, TenantName);
    protected IAlertManagerGrain AlertManager => GrainFactory.GetGrain<IAlertManagerGrain>(AlertManagerId);
    private FlownodesId EventBookId => new(FlownodesEntity.EventBook, TenantName);
    protected IEventBookGrain EventBook => GrainFactory.GetGrain<IEventBookGrain>(EventBookId);

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (!Metadata.RecordExists) Metadata.State["created_at"] = DateTime.Now;

        _logger.LogInformation("Activated resource grain {@ResourceId}", Id);
        return Task.CompletedTask;
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivated resource grain {@ResourceId} for reason {Reason}", Id, reason.Description);
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    public ValueTask<FlownodesId> GetId()
    {
        return ValueTask.FromResult(Id);
    }

    public ValueTask<Dictionary<string, object?>> GetMetadata()
    {
        _logger.LogDebug("Retrieved metadata of resource {@ResourceId}", Id);
        return ValueTask.FromResult(Metadata.State);
    }

    protected virtual Task OnUpdateMetadataAsync(Dictionary<string, object?> metadata)
    {
        return Task.CompletedTask;
    }

    public async Task UpdateMetadataAsync(Dictionary<string, object?> metadata)
    {
        await WriteMetadataAsync(metadata);
        await OnUpdateMetadataAsync(metadata);
    }

    protected async Task WriteMetadataAsync(Dictionary<string, object?> metadata)
    {
        Metadata.State = metadata;
        await Metadata.WriteStateAsync();

        await EventBook.RegisterEventAsync(EventKind.UpdateResourceMetadata, Id);
        _logger.LogInformation("Wrote metadata to resource store {@ResourceId}", Id);
    }

    protected async Task WriteMetadataConditionalAsync(Dictionary<string, object?> metadata)
    {
        if (Metadata.State.ContainsAll(metadata)) return;
        await WriteMetadataAsync(metadata);
    }

    public async Task ClearMetadataAsync()
    {
        if (!Metadata.RecordExists) return;
        await Metadata.ClearStateAsync();
        _logger.LogInformation("Cleared metadata of Resource {@ResourceId}", Id);
    }

    public virtual async Task SelfRemoveAsync()
    {
        await ClearMetadataAsync();
        _logger.LogInformation("Removed ResourceGrain {@ResourceId}", Id);
    }
}