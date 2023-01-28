using System.Collections.ObjectModel;
using Flownodes.Shared.Attributes;
using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;
using Flownodes.Worker.Models;
using Orleans.Runtime;
using Throw;

namespace Flownodes.Worker.Implementations;

public sealed class ResourceManagerGrain : Grain, IResourceManagerGrain
{
    private readonly IGrainFactory _grainFactory;

    private readonly ILogger<ResourceManagerGrain> _logger;
    private readonly IPersistentState<ResourceManagerPersistence> _persistence;

    public ResourceManagerGrain(ILogger<ResourceManagerGrain> logger,
        [PersistentState("resourceManagerState")]
        IPersistentState<ResourceManagerPersistence> persistence, IGrainFactory grainFactory)
    {
        _logger = logger;
        _persistence = persistence;
        _grainFactory = grainFactory;
        _grainFactory.GetGrain<IAlertManagerGrain>("alerter");
    }

    private string Id => this.GetPrimaryKeyString();

    public async ValueTask<Resource?> GetResourceSummary(string id)
    {
        id.ThrowIfNull().IfWhiteSpace();

        if (!id.Contains('/')) id = $"{Id}/{id}";

        var registration = _persistence.State.Registrations.SingleOrDefault(x => x.ResourceId.Equals(id));
        if (registration is null) return default;

        var grain = _grainFactory.GetGrain(registration.GrainId).AsReference<IResourceGrain>();
        var summary = await grain.GetPoco();

        _logger.LogDebug("Retrieved resource summary with FRN {Id}", id);
        return summary;
    }

    public async ValueTask<IResourceGrain?> GetResourceAsync(string id)
    {
        id.ThrowIfNull().IfWhiteSpace();

        if (!id.Contains('/')) id = $"{Id}/{id}";

        var registration = _persistence.State.Registrations.SingleOrDefault(x => x.ResourceId.Equals(id));
        if (registration is null) return default;

        var grain = _grainFactory.GetGrain(registration.GrainId).AsReference<IResourceGrain>();
        var frn = await grain.GetFrn();

        _logger.LogDebug("Retrieved resource with FRN {Frn}", frn);
        return grain;
    }

    public async ValueTask<ReadOnlyCollection<Resource>> GetAllResourceSummaries()
    {
        var summaries = new List<Resource>();

        var grains = _persistence.State.Registrations
            .Select(registration => _grainFactory.GetGrain(registration.GrainId).AsReference<IResourceGrain>());
        foreach (var grain in grains)
        {
            var summary = await grain.GetPoco();
            summaries.Add(summary);
        }

        return summaries.AsReadOnly();
    }

    public async ValueTask<TResourceGrain?> GetResourceAsync<TResourceGrain>(string id)
        where TResourceGrain : IResourceGrain
    {
        id.ThrowIfNull().IfWhiteSpace();

        if (!id.Contains('/')) id = $"{Id}/{id}";

        if (!_persistence.State.Registrations.Any(x => x.ResourceId.Equals(id)))
        {
            _logger.LogError("Could not find a resource with ID {Id}", id);
            return default;
        }

        var grain = _grainFactory.GetGrain<TResourceGrain>(id);

        var frn = await grain.GetFrn();

        _logger.LogDebug("Retrieved resource with FRN {Frn}", frn);
        return grain;
    }

    public async ValueTask<IResourceGrain?> GetGenericResourceAsync(string id)
    {
        id.ThrowIfNull().IfWhiteSpace();

        if (!id.Contains('/')) id = $"{Id}/{id}";

        var registration = _persistence.State.Registrations.FirstOrDefault(x => x.ResourceId.Equals(id));

        if (registration is null)
        {
            _logger.LogError("Could not find a resource with ID {Id}", id);
            return default;
        }

        var grain = _grainFactory.GetGrain(registration.GrainId).AsReference<IResourceGrain>();

        var frn = await grain.GetFrn();

        _logger.LogDebug("Retrieved resource with FRN {Frn}", frn);
        return grain;
    }

    public async ValueTask<TResourceGrain> DeployResourceAsync<TResourceGrain>(string id,
        ResourceConfigurationStore configurationStore) where TResourceGrain : IResourceGrain
    {
        id.ThrowIfNull().IfWhiteSpace();
        configurationStore.ThrowIfNull();

        if (!id.Contains('/')) id = $"{Id}/{id}";

        if (_persistence.State.Registrations.Any(x => x.ResourceId.Equals(id)))
            throw new InvalidOperationException($"Resource with ID {id} already exists");

        var grain = _grainFactory.GetGrain<TResourceGrain>(id);
        var kind = await grain.GetKind();

        // TODO: Verify if singleton is needed.
        if (Attribute.IsDefined(typeof(TResourceGrain), typeof(SingletonResourceAttribute)))
            _persistence.State.IsKindRegistered(kind)
                .Throw($"Singleton resource of Kind {kind} already exists")
                .IfFalse();

        await grain.UpdateConfigurationAsync(configurationStore);
        var frn = await grain.GetFrn();

        _persistence.State.AddRegistration(id, grain.GetGrainId(), kind, frn);
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Deployed resource with FRN {Frn}", frn);
        return grain;
    }

    public async Task RemoveResourceAsync(string id)
    {
        id.ThrowIfNull().IfWhiteSpace();

        if (!id.Contains('/')) id = $"{Id}/{id}";

        var registration = _persistence.State.Registrations.FirstOrDefault(x => x.ResourceId.Equals(id));
        if (registration is null)
            throw new InvalidOperationException($"Resource with ID {id} does not exist");

        var grain = _grainFactory.GetGrain(registration.GrainId).AsReference<IResourceGrain>();
        await grain.SelfRemoveAsync();

        var frn = await grain.GetFrn();

        _persistence.State.Registrations.Remove(registration);
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Removed resource with FRN {Frn}", frn);
    }

    public async Task RemoveAllResourcesAsync()
    {
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