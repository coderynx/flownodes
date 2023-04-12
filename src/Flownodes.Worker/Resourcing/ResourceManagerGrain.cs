using System.Collections.ObjectModel;
using Flownodes.Sdk.Entities;
using Flownodes.Shared.Eventing;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Resourcing.Exceptions;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Worker.Builders;
using Flownodes.Worker.Resourcing.Persistence;
using Orleans.Runtime;

namespace Flownodes.Worker.Resourcing;

[GrainType(FlownodesEntityNames.ResourceManager)]
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

    private FlownodesId Id => (FlownodesId)this.GetPrimaryKeyString();
    private string TenantName => Id.FirstName;
    private FlownodesId EventBookId => new(FlownodesEntity.EventBook, TenantName);
    private IEventBookGrain EventBook => GrainFactory.GetGrain<IEventBookGrain>(EventBookId);

    public async ValueTask<BaseResourceSummary?> GetResourceSummary(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var registration = _persistence.State.GetRegistration(name);
        if (registration is null) return default;

        var grain = _grainFactory.GetGrain(registration.GrainId).AsReference<IResourceGrain>();
        var summary = await grain.GetSummary();

        _logger.LogDebug("Retrieved resource summary of resource {@ResourceId}", summary.Id);
        return summary;
    }

    public async ValueTask<ReadOnlyCollection<BaseResourceSummary>> GetAllResourceSummaries()
    {
        var summaries = await _persistence.State.Registrations
            .Select(registration => _grainFactory.GetGrain(registration.GrainId).AsReference<IResourceGrain>())
            .ToAsyncEnumerable()
            .SelectAwait(async grain => await grain.GetSummary())
            .ToListAsync();

        return summaries.AsReadOnly();
    }

    public ValueTask<bool> IsResourceRegistered(string name)
    {
        return ValueTask.FromResult(_persistence.State.IsResourceRegistered(name));
    }

    public ValueTask<TResourceGrain?> GetResourceAsync<TResourceGrain>(string name)
        where TResourceGrain : IResourceGrain
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (!_persistence.State.IsResourceRegistered(name))
        {
            _logger.LogError("Could not find a resource with name {@ResourceName} of tenant {@TenantName}",
                name,
                TenantName);
            return default;
        }

        var id = FlownodesIdBuilder.CreateFromType(typeof(TResourceGrain), TenantName, name);
        var grain = _grainFactory.GetGrain<TResourceGrain>(id);

        _logger.LogDebug("Retrieved resource {@ResourceId}", id);
        return ValueTask.FromResult<TResourceGrain?>(grain);
    }

    public async ValueTask<IResourceGrain?> GetResourceAsync(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var registration = _persistence.State.GetRegistration(name);
        if (registration is null)
        {
            _logger.LogError("Could not find resource {@ResourceName} in tenant {@TenantName}", name,
                TenantName);
            return default;
        }

        var grain = _grainFactory.GetGrain(registration.GrainId).AsReference<IResourceGrain>();

        var id = await grain.GetId();
        _logger.LogDebug("Retrieved resource {@ResourceId}", id);

        return grain;
    }

    public ValueTask<IReadOnlyList<IResourceGrain>> SearchResourcesByTags(HashSet<string> tags)
    {
        if (tags.Count is 0)
            throw new ArgumentException("The tags set cannot be empty");

        var resources = _persistence.State.Registrations
            .Where(x => x.Tags.Any(y => tags.Any(s => s.Equals(y))))
            .Select(registration => _grainFactory.GetGrain(registration.GrainId).AsReference<IResourceGrain>())
            .ToList();

        _logger.LogDebug("Searched for resources in tenant {@TenantName} with tags {@Tags}", TenantName, tags);
        return ValueTask.FromResult<IReadOnlyList<IResourceGrain>>(resources);
    }

    public async ValueTask<TResourceGrain> DeployResourceAsync<TResourceGrain>(string name) where TResourceGrain : IResourceGrain
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        
        if (_persistence.State.IsResourceRegistered(name))
            throw new ResourceAlreadyRegisteredException(TenantName, name);
        
        var id = FlownodesIdBuilder.CreateFromType(typeof(TResourceGrain), TenantName, name);
        var kind = id.ToEntityKindString();
        
        // TODO: Further investigation for singleton resource is needed.
        if (_persistence.State.IsSingletonResourceRegistered<TResourceGrain>(kind))
            throw new SingletonResourceAlreadyRegistered(TenantName, name);
        
        var grain = _grainFactory.GetGrain<TResourceGrain>(id);
        var tags = new HashSet<string> { name, kind };

        _persistence.State.AddRegistration(name, grain.GetGrainId(), kind, tags);
        await _persistence.WriteStateAsync();

        await EventBook.RegisterEventAsync(EventKind.DeployedResource, Id);
        
        _logger.LogInformation("Deployed resource {@ResourceId}", id);

        return grain;
    }

    public async ValueTask<TResourceGrain> DeployResourceAsync<TResourceGrain>(string name,
        Dictionary<string, object?>? configuration, Dictionary<string, object?>? metadata = null)
        where TResourceGrain : IResourceGrain
    {
        var grain = await DeployResourceAsync<TResourceGrain>(name);
        
        if (await grain.GetIsConfigurable())
        {
            configuration ??= new Dictionary<string, object?>();

            var configurableGrain = (IConfigurableResourceGrain)grain;
            await configurableGrain.UpdateConfigurationAsync(configuration);
        }

        if (metadata is not null) await grain.UpdateMetadataAsync(metadata);
        
        return grain;
    }

    public async Task RemoveResourceAsync(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var registration = _persistence.State.GetRegistration(name);
        if (registration is null) throw new ResourceNotFoundException(TenantName, name);

        var grain = _grainFactory.GetGrain(registration.GrainId).AsReference<IResourceGrain>();
        var id = await grain.GetId();
        await grain.SelfRemoveAsync();

        _persistence.State.Registrations.Remove(registration);
        await _persistence.WriteStateAsync();

        await EventBook.RegisterEventAsync(EventKind.RemovedResource, Id);
        _logger.LogInformation("Removed resource {@ResourceId}", id);
    }

    public async Task RemoveAllResourcesAsync()
    {
        var grains = _persistence.State.Registrations
            .Select(registration => _grainFactory.GetGrain(registration.GrainId).AsReference<IResourceGrain>());

        foreach (var grain in grains) await grain.SelfRemoveAsync();

        _persistence.State.Registrations.Clear();
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Removed all resources of resource manager {@ResourceManagerId}", Id);
    }

    public ValueTask<FlownodesId> GetId()
    {
        return ValueTask.FromResult(Id);
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Resource manager {@ResourceManagerId} activated", Id);
        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Resource manager {@ResourceManagerId} deactivated", Id);
        return base.OnDeactivateAsync(reason, cancellationToken);
    }
}