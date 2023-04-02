using Flownodes.Sdk.Entities;
using Flownodes.Shared.Resourcing.Exceptions;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Worker.Builders;
using Flownodes.Worker.Services;
using Orleans.Runtime;

namespace Flownodes.Worker.Resourcing;

[GrainType(FlownodesEntityNames.ResourceGroup)]
internal sealed class ResourceGroupGrain : ResourceGrain, IResourceGroupGrain
{
    private readonly ILogger<ResourceGroupGrain> _logger;
    private readonly IPersistentState<HashSet<string>> _store;

    public ResourceGroupGrain(ILogger<ResourceGroupGrain> logger, IEnvironmentService environmentService,
        [PersistentState("resourceGroupStore")]
        IPersistentState<HashSet<string>> store, IPersistentStateFactory stateFactory, IGrainContext grainContext) :
        base(logger, environmentService, null, stateFactory, grainContext)
    {
        _logger = logger;
        _store = store;
    }

    public async Task RegisterResourceAsync(string name)
    {
        var resource = await ResourceManager.GetResourceAsync(name);
        if (resource is null) throw new ResourceNotFoundException(TenantName, name);

        var id = await resource.GetId();
        _store.State.Add(id);
        await _store.WriteStateAsync();

        _logger.LogInformation("Registered resource {@ResourceId} in resource group {@ResourceGroupId}", Id, id);
    }

    public async Task UnregisterResourceAsync(string name)
    {
        var resource = await ResourceManager.GetResourceAsync(name);
        if (resource is null) throw new ResourceNotFoundException(TenantName, name);

        var id = await resource.GetId();
        _store.State.Remove(id);
        await _store.WriteStateAsync();

        _logger.LogInformation("Unregistered resource {@ResourceId} from resource group {@ResourceGroupId}", id, Id);
    }

    public async ValueTask<TResourceGrain?> GetResourceAsync<TResourceGrain>(string name)
        where TResourceGrain : IResourceGrain
    {
        var id = FlownodesIdBuilder.CreateFromType(typeof(TResourceGrain), TenantName, name);
        if (!_store.State.Contains(id)) return default;

        return await ResourceManager.GetResourceAsync<TResourceGrain>(id.SecondName!);
    }

    public async ValueTask<IResourceGrain?> GetResourceAsync(string name)
    {
        var id = _store.State
            .Select(x => (FlownodesId)x)
            .SingleOrDefault(x => x.SecondName!.Equals(name));
        if (id is null) return default;

        return await ResourceManager.GetResourceAsync(name);
    }

    public ValueTask<bool> IsResourceRegistered(string name)
    {
        return ValueTask.FromResult(_store.State.Select(x => (FlownodesId)x).Any(x => x.SecondName!.Equals(name)));
    }

    public ValueTask<HashSet<FlownodesId>> GetRegistrations()
    {
        return ValueTask.FromResult(_store.State.Select(x => (FlownodesId)x).ToHashSet());
    }

    public async Task ClearRegistrationsAsync()
    {
        await _store.ClearStateAsync();
        _logger.LogInformation("Cleared registrations of resource group {@ResourceGroupId}", Id);
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activated resource group grain {@ResourceGroupId}", Id);
        return Task.CompletedTask;
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivated resource group grain {@ResourceGroupId}", Id);
        return Task.CompletedTask;
    }
}