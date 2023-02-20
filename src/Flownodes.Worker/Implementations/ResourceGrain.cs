using Flownodes.Sdk.Resourcing;
using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;
using Flownodes.Worker.Extensions;
using Flownodes.Worker.Services;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

[Reentrant]
public abstract class ResourceGrain : Grain
{
    private readonly IBehaviourProvider _behaviourProvider;
    private readonly IPersistentState<ResourceConfigurationStore> _configurationStore;
    private readonly IPersistentState<ResourceMetadataStore> _metadataStore;
    private readonly IPersistentState<ResourceStateStore> _stateStore;
    protected readonly IEnvironmentService EnvironmentService;
    protected readonly ILogger<ResourceGrain> Logger;
    protected IBehaviour? Behaviour;

    protected ResourceGrain(ILogger<ResourceGrain> logger, IEnvironmentService environmentService,
        IBehaviourProvider behaviourProvider, IPersistentState<ResourceConfigurationStore> configurationStore,
        IPersistentState<ResourceMetadataStore> metadataStore, IPersistentState<ResourceStateStore> stateStore)
    {
        Logger = logger;
        EnvironmentService = environmentService;
        _behaviourProvider = behaviourProvider;
        _configurationStore = configurationStore;
        _metadataStore = metadataStore;
        _stateStore = stateStore;
    }

    protected string Kind => this.GetGrainId().Type.ToString()!;
    protected string Id => this.GetPrimaryKeyString();
    protected string? BehaviourId => Configuration.BehaviourId;
    protected DateTime CreatedAt => Metadata.CreatedAt;
    protected IResourceManagerGrain ResourceManagerGrain => EnvironmentService.GetResourceManagerGrain();

    protected ResourceMetadataStore Metadata
    {
        get => _metadataStore.State;
        private set => _metadataStore.State = value;
    }

    protected ResourceConfigurationStore Configuration
    {
        get => _configurationStore.State;
        private set => _configurationStore.State = value;
    }

    protected ResourceStateStore State
    {
        get => _stateStore.State;
        private set => _stateStore.State = value;
    }

    public ValueTask<Resource> GetPoco()
    {
        Logger.LogDebug("Retrieved POCO of resource {ResourceId}", Id);
        return ValueTask.FromResult(new Resource(Id, CreatedAt, Configuration, Metadata, State));
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Activated resource grain {ResourceId}", Id);
        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Deactivated resource grain {ResourceId} for reason {Reason}", Id, reason.Description);
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    public ValueTask<string> GetKind()
    {
        return ValueTask.FromResult(Kind);
    }

    public ValueTask<string> GetId()
    {
        return ValueTask.FromResult(Id);
    }

    public ValueTask<ResourceMetadataStore> GetMetadata()
    {
        Logger.LogInformation("Retrieved metadata of resource {ResourceId}", Id);
        return ValueTask.FromResult(Metadata);
    }

    public ValueTask<ResourceConfigurationStore> GetConfiguration()
    {
        Logger.LogInformation("Retrieved configuration of resource {ResourceId}", Id);
        return ValueTask.FromResult(Configuration);
    }

    public virtual ValueTask<ResourceStateStore> GetState()
    {
        Logger.LogDebug("Retrieved state of resource {ResourceId}", Id);
        return ValueTask.FromResult(State);
    }

    protected ResourceContext GetResourceContext()
    {
        return new ResourceContext
        {
            ServiceId = EnvironmentService.ServiceId,
            ClusterId = EnvironmentService.ClusterId,
            TenantId = "", // TODO: Add tenant id.
            ResourceId = Id,
            CreatedAt = Metadata.CreatedAt,
            BehaviorId = Configuration.BehaviourId,
            Configuration = Configuration.Properties,
            Metadata = Metadata.Properties,
            State = State.Properties,
            LastStateUpdate = State.LastUpdate
        };
    }

    public async Task UpdateConfigurationAsync(Dictionary<string, object?>? configuration)
    {
        Configuration.Properties = configuration;
        await StoreConfigurationAsync();
    }

    public async Task UpdateConfigurationAsync(Dictionary<string, object?>? properties, string behaviorId)
    {
        Configuration.Properties = properties ?? new Dictionary<string, object?>();
        Configuration.BehaviourId = behaviorId;
        await StoreConfigurationAsync();
    }
    
    public virtual async Task UpdateConfigurationAsync(ResourceConfigurationStore configurationStore)
    {
        ArgumentNullException.ThrowIfNull(configurationStore);
        Configuration = configurationStore;

        if (BehaviourId is not null)
        {
            Behaviour = _behaviourProvider.GetBehaviour(BehaviourId);
            ArgumentNullException.ThrowIfNull(Behaviour);

            var context = GetResourceContext();
            await Behaviour.OnSetupAsync(context);

            await OnBehaviourUpdateAsync();
        }

        await StoreConfigurationAsync();
    }

    protected virtual Task OnStateChangedAsync(Dictionary<string, object?> newState)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnBehaviourUpdateAsync()
    {
        return Task.CompletedTask;
    }

    public async Task UpdateStateAsync(Dictionary<string, object?> newState)
    {
        State.Properties.MergeInPlace(newState);
        State.LastUpdate = DateTime.Now;

        await OnStateChangedAsync(newState);
        await StoreStateAsync();
    }

    public virtual async Task UpdateMetadataAsync(Dictionary<string, string?> metadata)
    {
        Metadata.Properties.MergeInPlace(metadata);
        await StoreMetadataAsync();
    }

    public virtual async Task ClearConfigurationAsync()
    {
        await _configurationStore.ClearStateAsync();
        Logger.LogInformation("Cleared configuration of resource {ResourceId}", Id);
    }

    public virtual async Task ClearMetadataAsync()
    {
        await _metadataStore.ClearStateAsync();
        Logger.LogInformation("Cleared metadata of resource {ResourceId}", Id);
    }

    public virtual async Task ClearStateAsync()
    {
        await _stateStore.ClearStateAsync();
        Logger.LogInformation("Cleared state of resource {ResourceId}", Id);
    }

    private async Task StoreConfigurationAsync()
    {
        await _configurationStore.WriteStateAsync();
        Logger.LogInformation("Updated configuration of resource {ResourceId}", Id);
    }

    private async Task StoreMetadataAsync()
    {
        await _metadataStore.WriteStateAsync();
        Logger.LogInformation("Updated metadata of resource {ResourceId}", Id);
    }

    private async Task StoreStateAsync()
    {
        await _stateStore.WriteStateAsync();
        Logger.LogInformation("Updated state of resource {ResourceId}", Id);
    }

    public virtual async Task SelfRemoveAsync()
    {
        await _configurationStore.ClearStateAsync();
        await _metadataStore.ClearStateAsync();
        await _stateStore.ClearStateAsync();

        Logger.LogInformation("Cleared persistence of resource {ResourceId}", Id);
    }
}