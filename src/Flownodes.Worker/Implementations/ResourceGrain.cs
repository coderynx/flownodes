using Flownodes.Sdk.Resourcing;
using Flownodes.Shared.Exceptions;
using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;
using Flownodes.Worker.Models;
using Flownodes.Worker.Services;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

[Reentrant]
internal abstract class ResourceGrain : Grain
{
    private readonly IPersistentState<ResourceConfigurationStore> _configurationStore;
    private readonly IPersistentState<ResourceMetadataStore> _metadataStore;
    private readonly IPluginProvider _pluginProvider;
    private readonly IPersistentState<ResourceStateStore> _stateStore;
    protected readonly IEnvironmentService EnvironmentService;
    protected readonly ILogger<ResourceGrain> Logger;
    protected IBehaviour? Behaviour;

    protected ResourceGrain(ILogger<ResourceGrain> logger, IEnvironmentService environmentService,
        IPluginProvider pluginProvider, IPersistentState<ResourceConfigurationStore> configurationStore,
        IPersistentState<ResourceMetadataStore> metadataStore, IPersistentState<ResourceStateStore> stateStore)
    {
        Logger = logger;
        EnvironmentService = environmentService;
        _pluginProvider = pluginProvider;
        _configurationStore = configurationStore;
        _metadataStore = metadataStore;
        _stateStore = stateStore;
    }

    protected string Kind => this.GetGrainId().Type.ToString()!;
    protected string Id => this.GetPrimaryKeyString();
    protected string TenantName => Id.Split('/')[0];
    protected string ResourceName => Id.Split('/')[1];

    protected string? BehaviourId
    {
        get => Configuration.Properties.GetValueOrDefault("behaviourId") as string;
        set => Configuration.Properties["behaviourId"] = value;
    }

    protected DateTime CreatedAt => Metadata.CreatedAt;
    protected IResourceManagerGrain ResourceManagerGrain => EnvironmentService.GetResourceManagerGrain();
    protected ResourceMetadataStore Metadata => _metadataStore.State;
    protected ResourceConfigurationStore Configuration => _configurationStore.State;
    protected ResourceStateStore State => _stateStore.State;

    public ValueTask<Resource> GetPoco()
    {
        Logger.LogDebug("Retrieved POCO of resource {ResourceId}", Id);
        return ValueTask.FromResult(new Resource(Id, TenantName, ResourceName, Kind, BehaviourId, CreatedAt,
            Configuration.Properties, Metadata.Properties, State.Properties, State.LastUpdate));
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

    public ValueTask<(Dictionary<string, string?> Proprties, DateTime CreatedAt)> GetMetadata()
    {
        Logger.LogInformation("Retrieved metadata of resource {ResourceId}", Id);
        return ValueTask.FromResult((Metadata.Properties, Metadata.CreatedAt));
    }

    public ValueTask<Dictionary<string, object?>> GetConfiguration()
    {
        Logger.LogInformation("Retrieved configuration of resource {ResourceId}", Id);
        return ValueTask.FromResult(Configuration.Properties);
    }

    public ValueTask<(Dictionary<string, object?> Properties, DateTime LastUpdate)> GetState()
    {
        Logger.LogDebug("Retrieved state of resource {@ResourceId}", Id);
        return ValueTask.FromResult((State.Properties, State.LastUpdate));
    }

    protected ResourceContext GetResourceContext()
    {
        return new ResourceContext(EnvironmentService.ServiceId, EnvironmentService.ClusterId, TenantName, ResourceName,
            Metadata.CreatedAt, BehaviourId, Configuration.Properties, Metadata.Properties,
            State.Properties);
    }

    public async Task UpdateConfigurationAsync(Dictionary<string, object?>? properties)
    {
        Configuration.UpdateProperties(properties);

        await GetRequiredBehaviour();

        await StoreConfigurationAsync();
    }

    private async Task GetRequiredBehaviour()
    {
        if (BehaviourId is null) return;

        Behaviour = _pluginProvider.GetBehaviour(BehaviourId);

        if (Behaviour is null) throw new BehaviourNotRegisteredException(BehaviourId);

        var context = GetResourceContext();
        await Behaviour.OnSetupAsync(context);

        await OnBehaviourUpdateAsync();
    }

    protected virtual Task OnStateChangedAsync(Dictionary<string, object?> newState)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnBehaviourUpdateAsync()
    {
        return Task.CompletedTask;
    }

    public async Task UpdateStateAsync(Dictionary<string, object?> state)
    {
        State.UpdateState(state);
        // await StoreStateAsync();
        await OnStateChangedAsync(state);
    }

    public virtual async Task UpdateMetadataAsync(Dictionary<string, string?> metadata)
    {
        Metadata.UpdateProperties(metadata);
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

    protected async Task StoreConfigurationAsync()
    {
        await _configurationStore.WriteStateAsync();
        Logger.LogInformation("Updated configuration of resource {ResourceId}", Id);
    }

    protected async Task StoreMetadataAsync()
    {
        await _metadataStore.WriteStateAsync();
        Logger.LogInformation("Updated metadata of resource {ResourceId}", Id);
    }

    protected async Task StoreStateAsync()
    {
        // TODO: Decide when to store the resource state.
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