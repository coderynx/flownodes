using Flownodes.Sdk;
using Flownodes.Sdk.Resourcing;
using Flownodes.Shared.Exceptions;
using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;
using Flownodes.Worker.Models;
using Flownodes.Worker.Services;
using Orleans.Concurrency;
using Orleans.EventSourcing;
using Orleans.Providers;

namespace Flownodes.Worker.Implementations;

[Reentrant]
[StorageProvider]
[LogConsistencyProvider]
internal abstract class ResourceGrain : JournaledGrain<ResourceGrainStore, IResourceGrainStoreEvent>
{
    private readonly IEnvironmentService _environmentService;
    private readonly IPluginProvider _pluginProvider;
    protected readonly ILogger<ResourceGrain> Logger;
    protected IBehaviour? Behaviour;

    protected ResourceGrain(ILogger<ResourceGrain> logger, IEnvironmentService environmentService,
        IPluginProvider pluginProvider)
    {
        Logger = logger;
        _environmentService = environmentService;
        _pluginProvider = pluginProvider;
    }

    protected string Kind => this.GetGrainId().Type.ToString()!;
    protected FlownodesId Id => this.GetPrimaryKeyString();
    protected string TenantName => Id.FirstName;
    protected string ResourceName => Id.SecondName!;
    protected bool IsConfigurable => GetType().IsAssignableTo(typeof(IConfigurableResource));
    protected bool IsStateful => GetType().IsAssignableTo(typeof(IStatefulResource));
    protected string? BehaviourId => State.Configuration?.GetValueOrDefault("behaviourId") as string;
    protected DateTime CreatedAt => State.CreatedAtDate;
    protected IResourceManagerGrain ResourceManager => _environmentService.GetResourceManagerGrain();
    protected IAlertManagerGrain AlertManager => _environmentService.GetAlertManagerGrain();

    public ValueTask<ResourceSummary> GetPoco()
    {
        Logger.LogDebug("Retrieved POCO of resource {@ResourceId}", Id);
        return ValueTask.FromResult(new ResourceSummary(Id, BehaviourId, CreatedAt, State.Configuration, State.Metadata,
            State.State, State?.LastStateUpdateDate));
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

        Logger.LogInformation("Activated resource grain {@ResourceId}", Id);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Deactivated resource grain {@ResourceId} for reason {Reason}", Id, reason.Description);
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    public ValueTask<string> GetKind()
    {
        return ValueTask.FromResult(Kind);
    }

    public ValueTask<string> GetId()
    {
        return ValueTask.FromResult<string>(Id);
    }

    public ValueTask<(Dictionary<string, string?> Metadata, DateTime? LastUpdateDate, DateTime CreatedAtDate)>
        GetMetadata()
    {
        Logger.LogInformation("Retrieved metadata of resource {@ResourceId}", Id);
        return ValueTask.FromResult((State.Metadata, State.LastMetadataUpdateDate, State.CreatedAtDate));
    }

    public ValueTask<(Dictionary<string, object?> Configuration, DateTime? LastUpdateDate)> GetConfiguration()
    {
        if (!IsConfigurable) throw new ResourceNotConfigurableException(TenantName, Kind, ResourceName);

        Logger.LogInformation("Retrieved configuration of resource {@ResourceId}", Id);
        return ValueTask.FromResult((State.Configuration, State.LastConfigurationUpdateDate))!;
    }

    public ValueTask<(Dictionary<string, object?> State, DateTime? LastUpdateDate)> GetState()
    {
        if (!IsStateful) throw new ResourceNotStatefulException(TenantName, Kind, ResourceName);

        Logger.LogDebug("Retrieved state of resource {@ResourceId}", Id);
        return ValueTask.FromResult((State.State, State.LastStateUpdateDate))!;
    }

    protected ResourceContext GetResourceContext()
    {
        return new ResourceContext(_environmentService.ServiceId, _environmentService.ClusterId, Id, CreatedAt, BehaviourId,
            IsConfigurable, State.Configuration, State.LastConfigurationUpdateDate, State.Metadata,
            State.LastMetadataUpdateDate, IsStateful, State.State, State.LastStateUpdateDate);
    }

    public async Task UpdateConfigurationAsync(Dictionary<string, object?> properties)
    {
        var @event = new UpdateResourceConfigurationEvent(properties);
        await RaiseConditionalEvent(@event);
        await GetRequiredBehaviour();

        Logger.LogInformation("Updated configuration of resource {@ResourceId}", Id);
    }

    public async Task ClearConfigurationAsync()
    {
        var @event = new ClearResourceConfigurationEvent();
        await RaiseConditionalEvent(@event);

        Logger.LogInformation("Cleared configuration of resource {@ResourceId}", Id);
    }

    private async Task GetRequiredBehaviour()
    {
        if (BehaviourId is null) return;

        Behaviour = _pluginProvider.GetBehaviour(BehaviourId);

        if (Behaviour is null) throw new BehaviourNotRegisteredException(BehaviourId);

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
        Logger.LogInformation("Updated state of resource {@ResourceId}", Id);
    }

    public async Task ClearStateAsync()
    {
        var @event = new ClearResourceStateEvent();
        await RaiseConditionalEvent(@event);

        Logger.LogInformation("Cleared state of resource {@ResourceId}", Id);
    }

    public virtual async Task UpdateMetadataAsync(Dictionary<string, string?> metadata)
    {
        var @event = new UpdateResourceMetadataEvent(metadata);
        await RaiseConditionalEvent(@event);

        Logger.LogInformation("Updated metadata of resource {@ResourceId}", Id);
    }

    public async Task ClearMetadataAsync()
    {
        var @event = new ClearResourceMetadataEvent();
        await RaiseConditionalEvent(@event);

        Logger.LogInformation("Cleared metadata of Resource {@ResourceId}", Id);
    }

    public virtual Task SelfRemoveAsync()
    {
        // TODO: Add clear state.
        Logger.LogInformation("Cleared persistence of resource {@ResourceId}", Id);
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

    protected override void TransitionState(ResourceGrainStore state, IResourceGrainStoreEvent @event)
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

        Logger.LogInformation("Raised event on {@ResourceId}", Id);
    }
}