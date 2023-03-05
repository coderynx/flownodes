using Flownodes.Shared.Exceptions;
using Flownodes.Shared.Interfaces;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

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
        
        if (await IsTenantRegistered(name))
        {
            var grain = _grainFactory.GetGrain<ITenantGrain>(name);

            _logger.LogDebug("Retrieved tenant with ID {Id}", name);
            return grain;
        }

        _logger.LogError("Could not retrieve tenant with ID {Id}", name);
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

        var grain = _grainFactory.GetGrain<ITenantGrain>(name);

        _logger.LogInformation("Created tenant with ID {Id}", name);
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

        _logger.LogInformation("Removed tenant with ID {Id}", name);
        return Task.CompletedTask;
    }
}