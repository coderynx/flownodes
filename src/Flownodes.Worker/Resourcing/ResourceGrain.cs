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

    protected ResourceGrain(ILogger<ResourceGrain> logger, IPersistentState<Dictionary<string, object?>> metadata)
    {
        _logger = logger;
        Metadata = metadata;
    }

    protected EntityId Id => (EntityId)this.GetPrimaryKeyString();
    protected string TenantName => Id.FirstName;
    private EntityId ResourceManagerId => new(Entity.ResourceManager, TenantName);
    protected IResourceManagerGrain ResourceManager => GrainFactory.GetGrain<IResourceManagerGrain>(ResourceManagerId);
    private EntityId AlertManagerId => new(Entity.AlertManager, TenantName);
    protected IAlertManagerGrain AlertManager => GrainFactory.GetGrain<IAlertManagerGrain>(AlertManagerId);
    private EntityId EventBookId => new(Entity.EventBook, TenantName);
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

    public ValueTask<EntityId> GetId()
    {
        return ValueTask.FromResult(Id);
    }

    public ValueTask<Dictionary<string, object?>> GetMetadata()
    {
        _logger.LogDebug("Retrieved metadata of resource {@ResourceId}", Id);
        return ValueTask.FromResult(Metadata.State);
    }

    public async Task UpdateMetadataAsync(Dictionary<string, object?> metadata)
    {
        await WriteMetadataAsync(metadata);
    }

    protected async Task WriteMetadataAsync(Dictionary<string, object?> metadata)
    {
        Metadata.State = metadata;
        await Metadata.WriteStateAsync();

        await EventBook.RegisterEventAsync(EventKind.UpdatedResource, Id);
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
}