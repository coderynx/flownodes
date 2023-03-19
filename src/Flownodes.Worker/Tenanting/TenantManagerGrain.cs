using Flownodes.Sdk;
using Flownodes.Shared.Tenanting;
using Flownodes.Shared.Tenanting.Exceptions;
using Orleans.Runtime;

namespace Flownodes.Worker.Tenanting;

[GrainType(FlownodesObjectNames.TenantManager)]
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

    public async ValueTask<ITenantGrain?> GetTenantAsync(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var id = new FlownodesId(FlownodesObject.Tenant, name);
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

        var id = new FlownodesId(FlownodesObject.Tenant, name);
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
}