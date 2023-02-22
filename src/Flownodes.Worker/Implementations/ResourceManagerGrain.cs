using System.Collections.ObjectModel;
using Flownodes.Shared.Exceptions;
using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;
using Flownodes.Worker.Models;
using Orleans.Runtime;

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
    }

    public async ValueTask<Resource?> GetResourceSummary(string tenantName, string resourceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantName);
        ArgumentException.ThrowIfNullOrEmpty(resourceName);

        var registration = _persistence.State.GetRegistration(tenantName, resourceName);
        if (registration is null) return default;

        var grain = _grainFactory.GetGrain(registration.GrainId).AsReference<IResourceGrain>();
        var summary = await grain.GetPoco();

        _logger.LogDebug("Retrieved resource summary of resource {ResourceName} of tenant {TenantName}", resourceName,
            tenantName);
        return summary;
    }

    public async ValueTask<ReadOnlyCollection<Resource>> GetAllResourceSummaries(string tenantName)
    {
        var summaries = new List<Resource>();

        var grains = _persistence.State
            .GetRegistrationsOfTenant(tenantName)
            .Select(registration => _grainFactory.GetGrain(registration.GrainId).AsReference<IResourceGrain>());

        foreach (var grain in grains)
        {
            var summary = await grain.GetPoco();
            summaries.Add(summary);
        }

        return summaries.AsReadOnly();
    }

    public ValueTask<TResourceGrain?> GetResourceAsync<TResourceGrain>(string tenantName, string resourceName)
        where TResourceGrain : IResourceGrain
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantName);
        ArgumentException.ThrowIfNullOrEmpty(resourceName);

        if (!_persistence.State.IsResourceRegistered(tenantName, resourceName))
        {
            _logger.LogError("Could not find a resource {ResourceName} of tenant {TenantName}", resourceName,
                tenantName);
            return default;
        }

        var grain = _grainFactory.GetGrain<TResourceGrain>($"{tenantName}/{resourceName}");

        _logger.LogDebug("Retrieved resource {ResourceName} of tenant {TenantName}", resourceName, tenantName);
        return ValueTask.FromResult<TResourceGrain?>(grain);
    }

    public ValueTask<IResourceGrain?> GetGenericResourceAsync(string tenantName, string resourceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantName);
        ArgumentException.ThrowIfNullOrEmpty(resourceName);

        var registration = _persistence.State.GetRegistration(tenantName, resourceName);
        if (registration is null)
        {
            _logger.LogError("Could not find resource {ResourceName} of tenant {TenantName}", resourceName, tenantName);
            return default;
        }

        var grain = _grainFactory.GetGrain(registration.GrainId).AsReference<IResourceGrain>();

        _logger.LogDebug("Retrieved resource {ResourceName} of tenant {TenantName}", resourceName, tenantName);
        return ValueTask.FromResult<IResourceGrain?>(grain);
    }

    public async ValueTask<TResourceGrain> DeployResourceAsync<TResourceGrain>(string tenantName, string resourceName,
        string behaviorId, Dictionary<string, object?>? configuration = null,
        Dictionary<string, string?>? metadata = null) where TResourceGrain : IResourceGrain
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantName);
        ArgumentException.ThrowIfNullOrEmpty(resourceName);
        ArgumentException.ThrowIfNullOrEmpty(behaviorId);

        if (_persistence.State.IsResourceRegistered(tenantName, resourceName))
            throw new ResourceAlreadyRegisteredException(tenantName, resourceName);

        var id = $"{tenantName}/{resourceName}";
        var grain = _grainFactory.GetGrain<TResourceGrain>(id);
        var kind = await grain.GetKind();

        // TODO: Further investigation for singleton resource is needed.
        if (_persistence.State.IsSingletonResourceRegistered<TResourceGrain>(kind))
            throw new SingletonResourceAlreadyRegistered(tenantName, resourceName);

        await grain.UpdateConfigurationAsync(configuration, behaviorId);

        if (metadata is not null) await grain.UpdateMetadataAsync(metadata);

        _persistence.State.AddRegistration(tenantName, resourceName, grain.GetGrainId(), kind);
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Deployed resource {ResourceName} in tenant {TenantName}", resourceName, tenantName);
        return grain;
    }

    public async Task RemoveResourceAsync(string tenantName, string resourceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantName);
        ArgumentException.ThrowIfNullOrEmpty(resourceName);

        var registration = _persistence.State.GetRegistration(tenantName, resourceName);
        if (registration is null)
            throw new ResourceNotFoundException(tenantName, resourceName);

        var grain = _grainFactory.GetGrain(registration.GrainId).AsReference<IResourceGrain>();
        await grain.SelfRemoveAsync();

        _persistence.State.Registrations.Remove(registration);
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Removed resource {ResourceId}", resourceName);
    }

    public async Task RemoveAllResourcesAsync(string tenantName)
    {
        var grains = _persistence.State
            .GetRegistrationsOfTenant(tenantName)
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