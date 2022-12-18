using Ardalis.GuardClauses;
using Flownodes.Core.Attributes;
using Flownodes.Core.Interfaces;
using Flownodes.Core.Models;
using Flownodes.Worker.Models;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

public sealed class ResourceManagerGrain : Grain, IResourceManagerGrain
{
    private readonly IGrainFactory _grainFactory;

    private readonly ILogger<ResourceManagerGrain> _logger;
    private readonly IPersistentState<ResourceManagerPersistence> _persistence;

    public ResourceManagerGrain(ILogger<ResourceManagerGrain> logger,
        [PersistentState("resourceManagerState", "flownodes")]
        IPersistentState<ResourceManagerPersistence> persistence, IGrainFactory grainFactory)
    {
        _logger = logger;
        _persistence = persistence;
        _grainFactory = grainFactory;
        _grainFactory.GetGrain<IAlerterGrain>("alerter");
    }

    public async ValueTask<TResourceGrain?> GetResourceAsync<TResourceGrain>(string id)
        where TResourceGrain : IResourceGrain
    {
        Guard.Against.NullOrWhiteSpace(id, nameof(id));

        if (_persistence.State.Registrations.FirstOrDefault(x => x.ResourceId.Equals(id)) is null)
        {
            _logger.LogError("Could not find a resource with ID {Id}", id);
            return default;
        }

        var grain = _grainFactory.GetGrain<TResourceGrain>(id);

        var frn = await grain.GetFrn();

        _logger.LogDebug("Retrieved resource with FRN {Frn}", frn);
        return grain;
    }

    public async ValueTask<TResourceGrain> DeployResourceAsync<TResourceGrain>(string id,
        ResourceConfiguration configuration) where TResourceGrain : IResourceGrain
    {
        Guard.Against.NullOrWhiteSpace(id, nameof(id));
        Guard.Against.Null(configuration, nameof(configuration));

        if (_persistence.State.Registrations.FirstOrDefault(x => x.GrainId.Equals(id)) is not null)
            throw new InvalidOperationException($"Resource with ID {id} already exists");

        var grain = _grainFactory.GetGrain<TResourceGrain>(id);
        var kind = await grain.GetKind();

        if (Attribute.IsDefined(typeof(TResourceGrain), typeof(SingletonResourceAttribute)))
            if (_persistence.State.Registrations.FirstOrDefault(x => x.Kind.Equals(kind)) is not null)
            {
                await grain.SelfRemoveAsync();
                throw new InvalidOperationException($"Singleton resource with Kind {kind} already exists");
            }

        await grain.UpdateConfigurationAsync(configuration);
        var frn = await grain.GetFrn();

        _persistence.State.AddRegistration(id, grain.GetGrainId(), kind, frn);
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Deployed resource with FRN {Frn}", frn);
        return grain;
    }

    public async Task RemoveResourceAsync(string id)
    {
        Guard.Against.NullOrWhiteSpace(id, nameof(id));

        var toRemove = _persistence.State.Registrations.FirstOrDefault(x => x.ResourceId.Equals(id));
        if (toRemove is null)
            throw new InvalidOperationException($"Resource with ID {id} does not exist");

        var grain = _grainFactory.GetGrain(toRemove.GrainId).AsReference<IResourceGrain>();
        await grain.SelfRemoveAsync();

        var frn = await grain.GetFrn();

        _persistence.State.Registrations.Remove(toRemove);
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Removed resource with FRN {Frn}", frn);
    }

    public async Task RemoveAllResourcesAsync()
    {
        var dummyName = typeof(DummyResourceGrain).AssemblyQualifiedName;

        var grains = _persistence.State.Registrations
            .Select(registration => _grainFactory.GetGrain(registration.GrainId).AsReference<IResourceGrain>());

        foreach (var grain in grains) await grain.SelfRemoveAsync();

        _persistence.State.Registrations.Clear();
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Removed all resources");
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Resource manager activated");
        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Resource manager deactivated");
        return base.OnDeactivateAsync(reason, cancellationToken);
    }
}