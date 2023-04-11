using Flownodes.Sdk.Entities;
using Flownodes.Shared.Alerting.Grains;
using Flownodes.Shared.Eventing;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Worker.Extensions;
using Flownodes.Worker.Resourcing.Persistence;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Flownodes.Worker.Resourcing;

[Reentrant]
internal abstract class ResourceGrain : Grain
{
    private readonly IPersistentState<BehaviourId> _behaviourId;
    private readonly IJournaledStoreGrain<Dictionary<string, object?>> _configuration = null!;
    private readonly ILogger<ResourceGrain> _logger;
    private readonly IPersistentState<Dictionary<string, object?>> _metadata;
    private readonly IJournaledStoreGrain<Dictionary<string, object?>> _state = null!;

    protected ResourceGrain(ILogger<ResourceGrain> logger, IPersistentStateFactory stateFactory,
        IGrainContext grainContext)
    {
        _logger = logger;
        _metadata = stateFactory.Create<Dictionary<string, object?>>(grainContext,
            new PersistentStateAttribute("resourceMetadataStore"));
        _behaviourId =
            stateFactory.Create<BehaviourId>(grainContext, new PersistentStateAttribute("resourceBehaviourIdStore"));

        if (IsConfigurable)
            _configuration =
                GrainFactory.GetGrain<IJournaledStoreGrain<Dictionary<string, object?>>>($"{Id}_configuration");

        if (IsStateful)
            _state =
                GrainFactory.GetGrain<IJournaledStoreGrain<Dictionary<string, object?>>>($"{Id}_state");
    }

    protected FlownodesId Id => (FlownodesId)this.GetPrimaryKeyString();
    protected string TenantName => Id.FirstName;
    private bool IsConfigurable => GetType().IsAssignableTo(typeof(IConfigurableResourceGrain));
    private bool IsStateful => GetType().IsAssignableTo(typeof(IStatefulResourceGrain));
    protected string? BehaviourId => _behaviourId.State.Value;
    protected Dictionary<string, object?> Metadata => _metadata.State;
    private FlownodesId ResourceManagerId => new(FlownodesEntity.ResourceManager, TenantName);
    protected IResourceManagerGrain ResourceManager => GrainFactory.GetGrain<IResourceManagerGrain>(ResourceManagerId);
    private FlownodesId AlertManagerId => new(FlownodesEntity.AlertManager, TenantName);
    protected IAlertManagerGrain AlertManager => GrainFactory.GetGrain<IAlertManagerGrain>(AlertManagerId);
    private FlownodesId EventBookId => new(FlownodesEntity.EventBook, TenantName);
    private IEventBookGrain EventBook => GrainFactory.GetGrain<IEventBookGrain>(EventBookId);

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (!_metadata.RecordExists) _metadata.State["created_at"] = DateTime.Now;

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
        return ValueTask.FromResult(_metadata.State);
    }

    public async ValueTask<Dictionary<string, object?>> GetConfiguration()
    {
        _logger.LogDebug("Retrieved configuration of resource {@ResourceId}", Id);
        return await _configuration.Get();
    }

    public async ValueTask<Dictionary<string, object?>> GetState()
    {
        _logger.LogDebug("Retrieved state of resource {@ResourceId}", Id);
        return await _state.Get();
    }

    public async Task UpdateConfigurationAsync(Dictionary<string, object?> properties)
    {
        await WriteConfigurationAsync(properties);
        await OnUpdateConfigurationAsync(properties);
    }

    protected async Task WriteConfigurationAsync(Dictionary<string, object?> properties)
    {
        await _configuration.UpdateAsync(properties);

        _metadata.State["configuration_updated_at"] = DateTime.Now;
        await _metadata.WriteStateAsync();

        await EventBook.RegisterEventAsync(EventKind.WroteResourceConfiguration, Id);
        _logger.LogInformation("Wrote configuration to resource store {@ResourceId}", Id);
    }

    protected async Task WriteConfigurationConditionalAsync(Dictionary<string, object?> configuration)
    {
        var storedConfiguration = await _configuration.Get();
        if (storedConfiguration.ContainsAll(configuration)) return;

        await _configuration.UpdateAsync(configuration);
    }

    public async Task ClearConfigurationAsync()
    {
        await _configuration.ClearAsync();
        _logger.LogInformation("Cleared configuration store of resource {@ResourceId}", Id);
    }

    public async Task UpdateBehaviourId(string behaviourId)
    {
        _behaviourId.State.Value = behaviourId;
        await _behaviourId.WriteStateAsync();
        await OnUpdateBehaviourAsync();

        _logger.LogInformation("Updated BehaviourId of ResourceGrain {@ResourceId}", Id);
    }

    protected virtual Task OnUpdateMetadataAsync(Dictionary<string, object?> metadata)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnUpdateConfigurationAsync(Dictionary<string, object?> configuration)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnUpdateStateAsync(Dictionary<string, object?> state)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnUpdateBehaviourAsync()
    {
        return Task.CompletedTask;
    }

    public async Task UpdateStateAsync(Dictionary<string, object?> state)
    {
        await WriteStateAsync(state);
        await OnUpdateStateAsync(state);
    }

    protected async Task WriteStateAsync(Dictionary<string, object?> state)
    {
        await _state.UpdateAsync(state);

        _metadata.State["state_updated_at"] = DateTime.Now;
        await _metadata.WriteStateAsync();

        await EventBook.RegisterEventAsync(EventKind.UpdateResourceState, Id);

        _logger.LogInformation("Wrote state for device {@DeviceId}", Id);
    }

    protected async Task WriteStateConditionalAsync(Dictionary<string, object?> state)
    {
        var storedState = await _state.Get();
        if (storedState.ContainsAll(state)) return;

        await WriteStateAsync(state);
    }

    public async Task ClearStateAsync()
    {
        await _state.ClearAsync();
        _logger.LogInformation("Cleared state of resource {@ResourceId}", Id);
    }

    public async Task UpdateMetadataAsync(Dictionary<string, object?> metadata)
    {
        await WriteMetadataAsync(metadata);
        await OnUpdateMetadataAsync(metadata);
    }

    protected async Task WriteMetadataAsync(Dictionary<string, object?> metadata)
    {
        _metadata.State = metadata;
        await _metadata.WriteStateAsync();

        await EventBook.RegisterEventAsync(EventKind.UpdateResourceMetadata, Id);
        _logger.LogInformation("Wrote metadata to resource store {@ResourceId}", Id);
    }

    protected async Task WriteMetadataConditionalAsync(Dictionary<string, object?> metadata)
    {
        if (_metadata.State.ContainsAll(metadata)) return;
        await WriteMetadataAsync(metadata);
    }

    public async Task ClearMetadataAsync()
    {
        await _metadata.ClearStateAsync();
        _logger.LogInformation("Cleared metadata of Resource {@ResourceId}", Id);
    }

    public virtual async Task SelfRemoveAsync()
    {
        await ClearMetadataAsync();
        await ClearConfigurationAsync();
        await ClearStateAsync();

        _logger.LogInformation("Removed ResourceGrain {@ResourceId}", Id);
    }

    public ValueTask<bool> GetIsConfigurable()
    {
        return ValueTask.FromResult(IsConfigurable);
    }

    public ValueTask<bool> GetIsStateful()
    {
        return ValueTask.FromResult(IsStateful);
    }
}