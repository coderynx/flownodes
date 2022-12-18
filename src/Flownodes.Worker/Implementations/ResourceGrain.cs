using Ardalis.GuardClauses;
using Flownodes.Core.Interfaces;
using Flownodes.Core.Models;
using Flownodes.Worker.Extensions;
using Flownodes.Worker.Models;
using Flownodes.Worker.Services;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

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
    protected string Frn => $"{EnvironmentService.BaseFrn}:{Kind}:{Id}";
    protected DateTime? CreatedAt => Persistence.State.CreatedAt;
    protected ResourceContext Context => new(Configuration, Metadata, State);

    protected Dictionary<string, string> Metadata
    {
        get => Persistence.State.Metadata;
        private set => Persistence.State.Metadata = value;
    }

    protected ResourceConfiguration? Configuration
    {
        get => Persistence.State.Configuration;
        private set => Persistence.State.Configuration = value;
    }

    protected ResourceState? State
    {
        get => Persistence.State.State;
        private set => Persistence.State.State = value;
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

    public virtual ValueTask<Dictionary<string, string>> GetMetadata()
    {
        return ValueTask.FromResult(Persistence.State.Metadata);
    }

    public ValueTask<ResourceConfiguration?> GetConfiguration()
    {
        return ValueTask.FromResult(Configuration);
    }

    public virtual async Task UpdateConfigurationAsync(ResourceConfiguration configuration)
    {
        Guard.Against.Null(configuration, nameof(configuration));

        Configuration = configuration;

        if (Configuration.BehaviourId is not null)
        {
            Behaviour = _behaviourProvider.GetBehaviour(configuration.BehaviourId);
            Guard.Against.Null(Behaviour, nameof(Behaviour));

            await Behaviour.OnSetupAsync(Context);

            Logger.LogInformation("Configured behaviour {BehaviourId} for resource {ResourceId}",
                configuration.BehaviourId, Id);
        }

        await Persistence.WriteStateAsync();

        Logger.LogInformation("Configured resource with FRN {Frn}", Frn);
    }

    public virtual async Task UpdateMetadataAsync(Dictionary<string, string> metadata)
    {
        Guard.Against.Null(metadata, nameof(metadata));

        Metadata.MergeInPlace(metadata);
        await Persistence.WriteStateAsync();

        Logger.LogInformation("Updated metadata for resource with FRN {Frn}", Frn);
    }

    public virtual async Task ClearConfigurationAsync()
    {
        Configuration = new ResourceConfiguration();
        await Persistence.WriteStateAsync();

        Behaviour = null;

        Logger.LogInformation("Cleared configuration for grain with FRN {Frn}", Frn);
    }

    public virtual async Task ClearMetadataAsync()
    {
        Metadata = new Dictionary<string, string>();
        await Persistence.WriteStateAsync();

        Logger.LogInformation("Cleared metadata for resource with FRN {Frn}", Frn);
    }

    public virtual ValueTask<ResourceState> GetState()
    {
        return ValueTask.FromResult(State);
    }

    public virtual async Task ClearStateAsync()
    {
        State = new ResourceState();
        await Persistence.WriteStateAsync();

        Logger.LogInformation("Cleared state for resource with FRN {Frn}", Frn);
    }

    public virtual async Task SelfRemoveAsync()
    {
        await Persistence.ClearStateAsync();
        Logger.LogInformation("Removed grain with FRN {Frn}", Frn);
    }
}