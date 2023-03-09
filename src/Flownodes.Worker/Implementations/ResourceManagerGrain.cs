using System.Collections.ObjectModel;
using Flownodes.Shared;
using Flownodes.Shared.Exceptions;
using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;
using Flownodes.Worker.Models;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

[GrainType(ObjectNames.ResourceManagerName)]
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
        ArgumentException.ThrowIfNullOrEmpty(tenantName);

        var summaries = await _persistence.State
            .GetRegistrationsOfTenant(tenantName)
            .Select(registration => _grainFactory.GetGrain(registration.GrainId).AsReference<IResourceGrain>())
            .ToAsyncEnumerable()
            .SelectAwait(async grain => await grain.GetPoco())
            .ToListAsync();

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

    public ValueTask<IReadOnlyList<IResourceGrain>> SearchResourcesByTags(string tenantName, HashSet<string> tags)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantName);

        if (tags.Count is 0)
            throw new ArgumentException("The tags set cannot be null");

        var resources = _persistence.State.Registrations
            .Where(x => x.TenantName.Equals(tenantName))
            .Where(x => x.Tags.Any(y => tags.Any(s => s.Equals(y))))
            .Select(registration => _grainFactory.GetGrain(registration.GrainId).AsReference<IResourceGrain>())
            .ToList();

        _logger.LogDebug("Searched for resources of tenants {@TenantName} with tags {@Tags}", tenantName, tags);
        return ValueTask.FromResult<IReadOnlyList<IResourceGrain>>(resources);
    }

    public async ValueTask<TResourceGrain> DeployResourceAsync<TResourceGrain>(string tenantName, string resourceName,
        Dictionary<string, object?>? configuration = null, Dictionary<string, string?>? metadata = null)
        where TResourceGrain : IResourceGrain
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantName);
        ArgumentException.ThrowIfNullOrEmpty(resourceName);

        if (_persistence.State.IsResourceRegistered(tenantName, resourceName))
            throw new ResourceAlreadyRegisteredException(tenantName, resourceName);

        var id = new FlownodesId(typeof(TResourceGrain), tenantName, resourceName);
        var grain = _grainFactory.GetGrain<TResourceGrain>(id);
        var kind = await grain.GetKind();
        var tags = new HashSet<string> { resourceName, kind };

        // TODO: Further investigation for singleton resource is needed.
        if (_persistence.State.IsSingletonResourceRegistered<TResourceGrain>(kind))
            throw new SingletonResourceAlreadyRegistered(tenantName, resourceName);

        if (await grain.GetIsConfigurable())
        {
            configuration ??= new Dictionary<string, object?>();

            var configurableGrain = (IConfigurableResource)grain;
            await configurableGrain.UpdateConfigurationAsync(configuration);

            if (configuration.GetValueOrDefault("behaviourId") is string behaviourId) tags.Add(behaviourId);
        }

        if (metadata is not null) await grain.UpdateMetadataAsync(metadata);

        _persistence.State.AddRegistration(tenantName, resourceName, grain.GetGrainId(), kind, tags);
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Deployed resource {ResourceName} in tenant {TenantName}", resourceName, tenantName);
        return grain;
    }

    public async Task RemoveResourceAsync(string tenantName, string resourceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantName);
        ArgumentException.ThrowIfNullOrEmpty(resourceName);

        var registration = _persistence.State.GetRegistration(tenantName, resourceName);
        if (registration is null) throw new ResourceNotFoundException(tenantName, resourceName);

        var grain = _grainFactory.GetGrain(registration.GrainId).AsReference<IResourceGrain>();
        await grain.SelfRemoveAsync();

        _persistence.State.Registrations.Remove(registration);
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Removed resource {ResourceId}", resourceName);
    }

    public async Task RemoveAllResourcesAsync(string tenantName)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantName);

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