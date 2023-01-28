using Flownodes.Sdk.Resourcing;
using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;
using Flownodes.Worker.Extensions;
using Flownodes.Worker.Services;
using Orleans.Concurrency;
using Orleans.Runtime;
using Throw;

namespace Flownodes.Worker.Implementations;

[Reentrant]
public abstract class ResourceGrain : Grain
{
    private readonly IBehaviourProvider _behaviourProvider;
    private readonly IPersistentState<ResourceConfigurationStore> ConfigurationStore;
    protected readonly IEnvironmentService EnvironmentService;
    protected readonly ILogger<ResourceGrain> Logger;
    private readonly IPersistentState<ResourceMetadataStore> MetadataStore;
    private readonly IPersistentState<ResourceStateStore> StateStore;
    protected IBehaviour? Behaviour;

    protected ResourceGrain(ILogger<ResourceGrain> logger, IEnvironmentService environmentService,
        IBehaviourProvider behaviourProvider, IPersistentState<ResourceConfigurationStore> configurationStore,
        IPersistentState<ResourceMetadataStore> metadataStore, IPersistentState<ResourceStateStore> stateStore)
    {
        Logger = logger;
        EnvironmentService = environmentService;
        _behaviourProvider = behaviourProvider;
        ConfigurationStore = configurationStore;
        MetadataStore = metadataStore;
        StateStore = stateStore;
    }

    protected string Kind => this.GetGrainId().Type.ToString()!;
    protected string Id => this.GetPrimaryKeyString();
    protected string? BehaviourId => Configuration.BehaviourId;
    protected string Frn => $"{EnvironmentService.BaseFrn}:{Kind}:{Id}";
    protected DateTime CreatedAt => Metadata.CreatedAt;
    protected IResourceManagerGrain ResourceManagerGrain => EnvironmentService.GetResourceManagerGrain();

    protected ResourceMetadataStore Metadata
    {
        get => MetadataStore.State;
        private set => MetadataStore.State = value;
    }

    protected ResourceConfigurationStore Configuration
    {
        get => ConfigurationStore.State;
        private set => ConfigurationStore.State = value;
    }

    protected ResourceStateStore State
    {
        get => StateStore.State;
        private set => StateStore.State = value;
    }

    public ValueTask<ResourceSummary> GetSummary()
    {
        Logger.LogDebug("Retrieved resource summary of resource {ResourceId}", Id);
        return ValueTask.FromResult(new ResourceSummary(Id, CreatedAt, Configuration, Metadata, State));
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

    public ValueTask<string> GetFrn()
    {
        return ValueTask.FromResult(Frn);
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
        var actualConfiguration = new ResourceConfiguration
        {
            BehaviourId = Configuration.BehaviourId,
            Properties = Configuration.Properties
        };
        var actualMetadata = new ResourceMetadata
        {
            CreatedAt = Metadata.CreatedAt,
            Properties = Metadata.Properties
        };
        var actualState = new ResourceState
        {
            Properties = State.Properties
        };

        return new ResourceContext(actualConfiguration, actualMetadata, actualState);
    }

    public virtual async Task UpdateConfigurationAsync(ResourceConfigurationStore configurationStore)
    {
        configurationStore.ThrowIfNull();
        Configuration = configurationStore;

        if (BehaviourId is not null)
        {
            Behaviour = _behaviourProvider.GetBehaviour(BehaviourId);
            Behaviour.ThrowIfNull();

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
        await ConfigurationStore.ClearStateAsync();
        Logger.LogInformation("Cleared configuration of resource {ResourceId}", Id);
    }

    public virtual async Task ClearMetadataAsync()
    {
        await MetadataStore.ClearStateAsync();
        Logger.LogInformation("Cleared metadata of resource {ResourceId}", Id);
    }

    public virtual async Task ClearStateAsync()
    {
        await StateStore.ClearStateAsync();
        Logger.LogInformation("Cleared state of resource {ResourceId}", Id);
    }

    private async Task StoreConfigurationAsync()
    {
        await ConfigurationStore.WriteStateAsync();
        Logger.LogInformation("Updated configuration of resource {ResourceId}", Id);
    }

    private async Task StoreMetadataAsync()
    {
        await MetadataStore.WriteStateAsync();
        Logger.LogInformation("Updated metadata of resource {ResourceId}", Id);
    }

    private async Task StoreStateAsync()
    {
        await StateStore.WriteStateAsync();
        Logger.LogInformation("Updated state of resource {ResourceId}", Id);
    }

    public virtual async Task SelfRemoveAsync()
    {
        await ConfigurationStore.ClearStateAsync();
        await MetadataStore.ClearStateAsync();
        await StateStore.ClearStateAsync();

        Logger.LogInformation("Cleared persistence of resource {ResourceId}", Id);
    }
}