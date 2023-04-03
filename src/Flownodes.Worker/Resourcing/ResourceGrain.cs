using Flownodes.Sdk.Entities;
using Flownodes.Sdk.Resourcing;
using Flownodes.Sdk.Resourcing.Behaviours;
using Flownodes.Shared.Alerting.Grains;
using Flownodes.Shared.Eventing;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Resourcing.Exceptions;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Worker.Extendability;
using Flownodes.Worker.Resourcing.Persistence;
using Flownodes.Worker.Services;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Flownodes.Worker.Resourcing;

[Reentrant]
internal abstract class ResourceGrain : Grain
{
    private readonly IEnvironmentService _environmentService;
    private readonly IExtensionProvider? _extensionProvider;
    private readonly IPersistentState<BehaviourId> _behaviourId;
    private readonly IPersistentState<ResourceMetadata> _metadata;
    private IJournaledStoreGrain<Dictionary<string, object?>> _configuration = null!;
    private IJournaledStoreGrain<Dictionary<string, object?>> _state = null!;
    private readonly ILogger<ResourceGrain> _logger;
    protected IBehaviour? Behaviour;

    protected ResourceGrain(ILogger<ResourceGrain> logger, IEnvironmentService environmentService,
        IExtensionProvider? extensionProvider, IPersistentStateFactory stateFactory, IGrainContext grainContext)
    {
        _logger = logger;
        _environmentService = environmentService;
        _extensionProvider = extensionProvider;
        _metadata = stateFactory.Create<ResourceMetadata>(grainContext,
            new PersistentStateAttribute("resourceMetadataStore"));
        _behaviourId = stateFactory.Create<BehaviourId>(grainContext, new PersistentStateAttribute("resourceBehaviourIdStore"));
    }

    protected FlownodesId Id => (FlownodesId)this.GetPrimaryKeyString();
    protected string TenantName => Id.FirstName;
    private bool IsConfigurable => GetType().IsAssignableTo(typeof(IConfigurableResourceGrain));
    private bool IsStateful => GetType().IsAssignableTo(typeof(IStatefulResourceGrain));
    private string? BehaviourId => _behaviourId.State.Value;
    private FlownodesId ResourceManagerId => new(FlownodesEntity.ResourceManager, TenantName);
    protected IResourceManagerGrain ResourceManager => GrainFactory.GetGrain<IResourceManagerGrain>(ResourceManagerId);
    private FlownodesId AlertManagerId => new(FlownodesEntity.AlertManager, TenantName);
    protected IAlertManagerGrain AlertManager => GrainFactory.GetGrain<IAlertManagerGrain>(AlertManagerId);
    private FlownodesId EventBookId => new(FlownodesEntity.EventBook, TenantName);
    private IEventBookGrain EventBook => GrainFactory.GetGrain<IEventBookGrain>(EventBookId);

    public async ValueTask<ResourceSummary> GetSummary()
    {
        _logger.LogDebug("Retrieved summary of resource {@ResourceId}", Id);

        var configuration = await _configuration.Get();
        var state = await _state.Get();
        var stateLastUpdate = await _state.GetUpdatedAt();

        var summary = new ResourceSummary(Id, BehaviourId, _metadata.State.CreatedAt, configuration,
            _metadata.State.Properties,
            state,
            stateLastUpdate);
        return summary;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (IsConfigurable)
        {
            _configuration =
                GrainFactory.GetGrain<IJournaledStoreGrain<Dictionary<string, object?>>>($"{Id}_configuration");
        }

        if (IsStateful)
        {
            _state =
                GrainFactory.GetGrain<IJournaledStoreGrain<Dictionary<string, object?>>>($"{Id}_state");
        }

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

    public ValueTask<(Dictionary<string, string?> Metadata, DateTime CreatedAtDate)> GetMetadata()
    {
        _logger.LogDebug("Retrieved metadata of resource {@ResourceId}", Id);
        return ValueTask.FromResult((_metadata.State.Properties, _metadata.State.CreatedAt));
    }

    public async ValueTask<(Dictionary<string, object?> Configuration, DateTime? LastUpdateDate)> GetConfiguration()
    {
        _logger.LogDebug("Retrieved configuration of resource {@ResourceId}", Id);
        return (await _configuration.Get(), await _configuration.GetUpdatedAt());
    }

    public async ValueTask<(Dictionary<string, object?> State, DateTime? LastUpdateDate)> GetState()
    {
        _logger.LogDebug("Retrieved state of resource {@ResourceId}", Id);
        return (await _state.Get(), await _state.GetUpdatedAt());
    }

    private async ValueTask<ResourceContext> GetResourceContextAsync()
    {
        return new ResourceContext(_environmentService.ServiceId, _environmentService.ClusterId, Id,
            _metadata.State.CreatedAt,
            BehaviourId, IsConfigurable, await _configuration.Get(), await _configuration.GetUpdatedAt(),
            _metadata.State.Properties, await _state.GetUpdatedAt(), IsStateful, await _state.Get(),
            await _state.GetUpdatedAt());
    }

    public async Task UpdateConfigurationAsync(Dictionary<string, object?> properties)
    {
        await WriteConfigurationAsync(properties);
        await OnUpdateConfigurationAsync(properties);
    }

    protected async Task WriteConfigurationAsync(Dictionary<string, object?> properties)
    {
        await _configuration.UpdateAsync(properties);
        await EventBook.RegisterEventAsync(EventKind.WroteResourceConfiguration, Id);
        _logger.LogInformation("Wrote configuration to resource store {@ResourceId}", Id);
    }

    public async Task ClearConfigurationAsync()
    {
        await _configuration.ClearAsync();
        _logger.LogInformation("Cleared configuration store of resource {@ResourceId}", Id);
    }

    private async Task GetRequiredBehaviour()
    {
        if (_extensionProvider is null || BehaviourId is null) return;

        var context = await GetResourceContextAsync();
        Behaviour = _extensionProvider.GetBehaviour(BehaviourId, context);

        if (Behaviour is null) throw new ResourceBehaviourNotRegisteredException(BehaviourId);

        await Behaviour.OnSetupAsync();
        await OnBehaviourChangeAsync();
    }

    public async Task UpdateBehaviourId(string behaviourId)
    {
        _behaviourId.State = new BehaviourId(behaviourId);
        await _behaviourId.WriteStateAsync();
        await GetRequiredBehaviour();
    }
    
    protected virtual Task OnUpdateMetadataAsync(Dictionary<string, string?> metadata)
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

    protected virtual Task OnBehaviourChangeAsync()
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
        await EventBook.RegisterEventAsync(EventKind.UpdateResourceState, Id);

        _logger.LogInformation("Wrote state for device {@DeviceId}", Id);
    }

    public async Task ClearStateAsync()
    {
        await _state.ClearAsync();
        _logger.LogInformation("Cleared state of resource {@ResourceId}", Id);
    }

    public async Task UpdateMetadataAsync(Dictionary<string, string?> metadata)
    {
        await WriteMetadataAsync(metadata);
        await OnUpdateMetadataAsync(metadata);
    }

    protected async Task WriteMetadataAsync(Dictionary<string, string?> metadata)
    {
        _metadata.State.Properties = metadata;
        await _metadata.WriteStateAsync();

        await EventBook.RegisterEventAsync(EventKind.UpdateResourceMetadata, Id);
        _logger.LogInformation("Wrote metadata to resource store {@ResourceId}", Id);
    }

    public async Task ClearMetadataAsync()
    {
        await _metadata.ClearStateAsync();
        _logger.LogInformation("Cleared metadata of Resource {@ResourceId}", Id);
    }

    public virtual Task SelfRemoveAsync()
    {
        // TODO: Add clear state.
        _logger.LogInformation("Cleared persistence of resource {@ResourceId}", Id);
        return Task.CompletedTask;
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