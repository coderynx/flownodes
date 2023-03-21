using Flownodes.Sdk.Entities;
using Flownodes.Sdk.Resourcing;
using Flownodes.Sdk.Resourcing.Behaviours;
using Flownodes.Shared.Alerting.Grains;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Resourcing.Exceptions;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Worker.Resourcing.Persistence;
using Flownodes.Worker.Services;
using Orleans.Concurrency;
using Orleans.EventSourcing;
using Orleans.Providers;

namespace Flownodes.Worker.Resourcing;

[Reentrant]
[StorageProvider]
[LogConsistencyProvider]
internal abstract class ResourceGrain : JournaledGrain<ResourceGrainPersistence, IResourceGrainPersistenceEvent>
{
    private readonly IEnvironmentService _environmentService;
    private readonly ILogger<ResourceGrain> _logger;
    private readonly IPluginProvider? _pluginProvider;
    protected IBehaviour? Behaviour;

    protected ResourceGrain(ILogger<ResourceGrain> logger, IEnvironmentService environmentService,
        IPluginProvider? pluginProvider)
    {
        _logger = logger;
        _environmentService = environmentService;
        _pluginProvider = pluginProvider;
    }

    protected string Kind => this.GetGrainId().Type.ToString()!;
    protected FlownodesId Id => (FlownodesId)this.GetPrimaryKeyString();
    protected string TenantName => Id.FirstName;
    protected string ResourceName => Id.SecondName!;
    protected bool IsConfigurable => GetType().IsAssignableTo(typeof(IConfigurableResourceGrain));
    protected bool IsStateful => GetType().IsAssignableTo(typeof(IStatefulResourceGrain));
    protected string? BehaviourId => State.Configuration?.GetValueOrDefault("behaviourId") as string;
    private FlownodesId ResourceManagerId => new(FlownodesEntity.ResourceManager, TenantName);
    protected IResourceManagerGrain ResourceManager => GrainFactory.GetGrain<IResourceManagerGrain>(ResourceManagerId);
    private FlownodesId AlertManagerId => new(FlownodesEntity.AlertManager, TenantName);
    protected IAlertManagerGrain AlertManager => GrainFactory.GetGrain<IAlertManagerGrain>(AlertManagerId);

    public ValueTask<ResourceSummary> GetSummary()
    {
        _logger.LogDebug("Retrieved summary of resource {@ResourceId}", Id);

        var summary = new ResourceSummary(Id, BehaviourId, State.CreatedAtDate, State.Configuration, State.Metadata,
            State.State,
            State.LastStateUpdateDate);
        return ValueTask.FromResult(summary);
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (IsConfigurable && State.Configuration is null)
        {
            var @event = new InitializeResourceConfigurationEvent();
            await RaiseConditionalEvent(@event);
        }

        if (IsStateful && State.State is null)
        {
            var @event = new InitializeResourceStateEvent();
            await RaiseConditionalEvent(@event);
        }

        _logger.LogInformation("Activated resource grain {@ResourceId}", Id);
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

    public ValueTask<(Dictionary<string, string?> Metadata, DateTime? LastUpdateDate, DateTime CreatedAtDate)>
        GetMetadata()
    {
        _logger.LogInformation("Retrieved metadata of resource {@ResourceId}", Id);
        return ValueTask.FromResult((State.Metadata, State.LastMetadataUpdateDate, State.CreatedAtDate));
    }

    public ValueTask<(Dictionary<string, object?> Configuration, DateTime? LastUpdateDate)> GetConfiguration()
    {
        _logger.LogInformation("Retrieved configuration of resource {@ResourceId}", Id);
        return ValueTask.FromResult((State.Configuration, State.LastConfigurationUpdateDate))!;
    }

    public ValueTask<(Dictionary<string, object?> State, DateTime? LastUpdateDate)> GetState()
    {
        _logger.LogDebug("Retrieved state of resource {@ResourceId}", Id);
        return ValueTask.FromResult((State.State, State.LastStateUpdateDate))!;
    }

    protected ResourceContext GetResourceContext()
    {
        return new ResourceContext(_environmentService.ServiceId, _environmentService.ClusterId, Id,
            State.CreatedAtDate,
            BehaviourId,
            IsConfigurable, State.Configuration, State.LastConfigurationUpdateDate, State.Metadata,
            State.LastMetadataUpdateDate, IsStateful, State.State, State.LastStateUpdateDate);
    }

    public async Task UpdateConfigurationAsync(Dictionary<string, object?> properties)
    {
        var @event = new UpdateResourceConfigurationEvent(properties);
        await RaiseConditionalEvent(@event);
        await GetRequiredBehaviour();

        _logger.LogInformation("Updated configuration of resource {@ResourceId}", Id);
    }

    public async Task ClearConfigurationAsync()
    {
        var @event = new ClearResourceConfigurationEvent();
        await RaiseConditionalEvent(@event);

        _logger.LogInformation("Cleared configuration of resource {@ResourceId}", Id);
    }

    private async Task GetRequiredBehaviour()
    {
        if (_pluginProvider is null || BehaviourId is null) return;

        Behaviour = _pluginProvider.GetBehaviour(BehaviourId);

        if (Behaviour is null) throw new ResourceBehaviourNotRegisteredException(BehaviourId);

        var context = GetResourceContext();
        await Behaviour.OnSetupAsync(context);

        await OnBehaviourChangedAsync();
    }

    protected virtual Task OnUpdateStateAsync(Dictionary<string, object?> newState)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnBehaviourChangedAsync()
    {
        return Task.CompletedTask;
    }

    public async Task UpdateStateAsync(Dictionary<string, object?> state)
    {
        var @event = new UpdateResourceStateEvent(state);
        await RaiseConditionalEvent(@event);

        await OnUpdateStateAsync(state);
        _logger.LogInformation("Updated state of resource {@ResourceId}", Id);
    }

    public async Task ClearStateAsync()
    {
        var @event = new ClearResourceStateEvent();
        await RaiseConditionalEvent(@event);

        _logger.LogInformation("Cleared state of resource {@ResourceId}", Id);
    }

    public virtual async Task UpdateMetadataAsync(Dictionary<string, string?> metadata)
    {
        var @event = new UpdateResourceMetadataEvent(metadata);
        await RaiseConditionalEvent(@event);

        _logger.LogInformation("Updated metadata of resource {@ResourceId}", Id);
    }

    public async Task ClearMetadataAsync()
    {
        var @event = new ClearResourceMetadataEvent();
        await RaiseConditionalEvent(@event);

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

    protected override void TransitionState(ResourceGrainPersistence state, IResourceGrainPersistenceEvent @event)
    {
        switch (@event)
        {
            case InitializeResourceConfigurationEvent:
                State.InitializeConfiguration();
                break;
            case UpdateResourceConfigurationEvent updateConfiguration:
                State.UpdateConfiguration(updateConfiguration.Configuration);
                break;
            case ClearResourceConfigurationEvent:
                State.ClearConfiguration();
                break;
            case UpdateResourceMetadataEvent updateMetadata:
                State.UpdateMetadata(updateMetadata.Metadata);
                break;
            case ClearResourceMetadataEvent:
                State.ClearMetadata();
                break;
            case InitializeResourceStateEvent:
                State.InitializeState();
                break;
            case UpdateResourceStateEvent updateState:
                State.UpdateState(updateState.State);
                break;
            case ClearResourceStateEvent:
                State.ClearState();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(@event));
        }

        _logger.LogInformation("Raised event on {@ResourceId}", Id);
    }
}