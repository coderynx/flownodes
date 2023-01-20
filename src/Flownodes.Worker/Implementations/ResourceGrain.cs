using Flownodes.Sdk.Resourcing;
using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;
using Flownodes.Worker.Extensions;
using Flownodes.Worker.Models;
using Flownodes.Worker.Services;
using Orleans.Concurrency;
using Orleans.Runtime;
using Throw;

namespace Flownodes.Worker.Implementations;

[Reentrant]
public abstract class ResourceGrain : Grain
{
    private readonly IBehaviourProvider _behaviourProvider;
    protected readonly IEnvironmentService EnvironmentService;
    protected readonly ILogger<ResourceGrain> Logger;
    protected readonly IPersistentState<ResourcePersistence> Persistence;
    protected IBehaviour? Behaviour;

    protected ResourceGrain(ILogger<ResourceGrain> logger, IPersistentState<ResourcePersistence> persistence,
        IEnvironmentService environmentService, IBehaviourProvider behaviourProvider)
    {
        Logger = logger;
        Persistence = persistence;
        EnvironmentService = environmentService;
        _behaviourProvider = behaviourProvider;
    }

    protected string Kind => this.GetGrainId().Type.ToString()!;
    protected string Id => this.GetPrimaryKeyString();
    protected string? BehaviourId => ConfigurationStore.BehaviourId;
    protected string Frn => $"{EnvironmentService.BaseFrn}:{Kind}:{Id}";
    protected DateTime CreatedAt => Persistence.State.Metadata.CreatedAt;
    protected IResourceManagerGrain ResourceManagerGrain => EnvironmentService.GetResourceManagerGrain();

    protected ResourceMetadataStore Metadata
    {
        get => Persistence.State.Metadata;
        private set => Persistence.State.Metadata = value;
    }

    protected ResourceConfigurationStore ConfigurationStore
    {
        get => Persistence.State.ConfigurationStore;
        private set => Persistence.State.ConfigurationStore = value;
    }

    protected ResourceStateStore StateStore
    {
        get => Persistence.State.StateStore;
        private set => Persistence.State.StateStore = value;
    }

    public ValueTask<ResourceSummary> GetSummary()
    {
        return ValueTask.FromResult(new ResourceSummary(Id, CreatedAt, ConfigurationStore, Metadata, StateStore));
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Activated grain {Frn}", Frn);
        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Deactivated grain {Frn} for reason {Reason}", Frn, reason.Description);
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
        return ValueTask.FromResult(Persistence.State.Metadata);
    }

    public ValueTask<ResourceConfigurationStore> GetConfiguration()
    {
        return ValueTask.FromResult(ConfigurationStore);
    }

    protected ResourceContext GetResourceContext()
    {
        var actualConfiguration = new ResourceConfiguration
        {
            BehaviourId = ConfigurationStore.BehaviourId,
            Properties = ConfigurationStore.Properties
        };
        var actualMetadata = new ResourceMetadata
        {
            CreatedAt = Metadata.CreatedAt,
            Properties = Metadata.Properties
        };
        var actualState = new ResourceState
        {
            Properties = StateStore.Properties
        };

        return new ResourceContext(actualConfiguration, actualMetadata, actualState);
    }

    public virtual async Task UpdateConfigurationAsync(ResourceConfigurationStore configurationStore)
    {
        configurationStore.ThrowIfNull();

        ConfigurationStore = configurationStore;

        if (BehaviourId is not null)
        {
            Behaviour = _behaviourProvider.GetBehaviour(BehaviourId);
            Behaviour.ThrowIfNull();

            var context = GetResourceContext();
            await Behaviour.OnSetupAsync(context);

            await OnBehaviourUpdateAsync();

            Logger.LogInformation("Configured behaviour {BehaviourId} for resource {ResourceId}",
                configurationStore.BehaviourId, Id);
        }

        await Persistence.WriteStateAsync();

        Logger.LogInformation("Configured resource with FRN {Frn}", Frn);
    }

    protected virtual Task OnBehaviourUpdateAsync()
    {
        return Task.CompletedTask;
    }

    public virtual async Task UpdateMetadataAsync(Dictionary<string, string?> metadata)
    {
        metadata.ThrowIfNull();

        Metadata.Properties.MergeInPlace(metadata);
        await Persistence.WriteStateAsync();

        Logger.LogInformation("Updated metadata for resource with FRN {Frn}", Frn);
    }

    public virtual async Task ClearConfigurationAsync()
    {
        ConfigurationStore = new ResourceConfigurationStore();
        await Persistence.WriteStateAsync();

        Behaviour = null;

        Logger.LogInformation("Cleared configuration for grain with FRN {Frn}", Frn);
    }

    public virtual async Task ClearMetadataAsync()
    {
        Metadata = new ResourceMetadataStore();
        await Persistence.WriteStateAsync();

        Logger.LogInformation("Cleared metadata for resource with FRN {Frn}", Frn);
    }

    public virtual ValueTask<ResourceStateStore> GetState()
    {
        return ValueTask.FromResult(StateStore);
    }

    public virtual async Task ClearStateAsync()
    {
        StateStore = new ResourceStateStore();
        await Persistence.WriteStateAsync();

        Logger.LogInformation("Cleared state for resource with FRN {Frn}", Frn);
    }

    public virtual async Task SelfRemoveAsync()
    {
        await Persistence.ClearStateAsync();
        Logger.LogInformation("Removed grain with FRN {Frn}", Frn);
    }
}