using Flownodes.Sdk.Entities;
using Flownodes.Shared.Tenanting.Exceptions;
using Flownodes.Shared.Tenanting.Grains;
using Orleans.Runtime;

namespace Flownodes.Worker.Tenanting;

[GrainType(EntityNames.TenantManager)]
public class TenantManagerGrain : ITenantManagerGrain
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<TenantManagerGrain> _logger;
    private readonly IPersistentState<HashSet<string>> _registrations;

    public TenantManagerGrain(ILogger<TenantManagerGrain> logger,
        [PersistentState("tenantRegistrations")]
        IPersistentState<HashSet<string>> registrations, IGrainFactory grainFactory)
    {
        _logger = logger;
        _registrations = registrations;
        _grainFactory = grainFactory;
    }

    private EntityId Id => (EntityId)this.GetPrimaryKeyString();

    public async ValueTask<ITenantGrain?> GetTenantAsync(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var id = new EntityId(Entity.Tenant, name);
        if (await IsTenantRegistered(name))
        {
            var grain = _grainFactory.GetGrain<ITenantGrain>(id);

            _logger.LogDebug("Retrieved tenant {@TenantId}", id);
            return grain;
        }

        _logger.LogError("Could not retrieve tenant {@TenantId}", id);
        return default;
    }

    public ValueTask<IList<ITenantGrain>> GetTenantsAsync()
    {
        var tenants = _registrations.State
            .Select(registration => _grainFactory.GetGrain<ITenantGrain>(registration))
            .ToList();

        _logger.LogDebug("Retrieved all tenants");
        return ValueTask.FromResult<IList<ITenantGrain>>(tenants);
    }

    public async ValueTask<ITenantGrain> CreateTenantAsync(string name, IDictionary<string, string?>? metadata = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (await IsTenantRegistered(name))
            throw new TenantAlreadyRegisteredException(name);

        _registrations.State.Add(name);
        await _registrations.WriteStateAsync();

        var id = new EntityId(Entity.Tenant, name);
        var grain = _grainFactory.GetGrain<ITenantGrain>(id);

        _logger.LogInformation("Created tenant with ID {@TenantId}", id);
        return grain;
    }

    public ValueTask<bool> IsTenantRegistered(string tenantName)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantName);

        return ValueTask.FromResult(_registrations.State.Contains(tenantName));
    }

    public Task RemoveTenantAsync(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        // TODO: Perform tenant removal.

        _logger.LogInformation("Removed tenant with ID {@TenantId}", name);
        return Task.CompletedTask;
    }

    public ValueTask<EntityId> GetId()
    {
        return ValueTask.FromResult(Id);
    }
}