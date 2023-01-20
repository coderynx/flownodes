using Flownodes.Shared.Interfaces;
using Orleans.Runtime;
using Throw;

namespace Flownodes.Worker.Implementations;

public class TenantManagerGrain : ITenantManagerGrain
{
    public TenantManagerGrain(ILogger<TenantManagerGrain> logger,
        [PersistentState("tenantRegistrations", "flownodes")]
        IPersistentState<HashSet<string>> registrations, IGrainFactory grainFactory)
    {
        _logger = logger;
        _registrations = registrations;
        _grainFactory = grainFactory;
    }

    private readonly ILogger<TenantManagerGrain> _logger;
    private readonly IPersistentState<HashSet<string>> _registrations;
    private readonly IGrainFactory _grainFactory;

    public ValueTask<ITenantGrain?> GetTenantAsync(string id)
    {
        if (_registrations.State.Contains(id))
        {
            var grain = _grainFactory.GetGrain<ITenantGrain>(id);
            
            _logger.LogDebug("Retrieved tenant with ID {Id}", id);
            return ValueTask.FromResult(grain);
        }

        _logger.LogError("Could not retrieve tenant with ID {Id}", id);
        return ValueTask.FromResult<ITenantGrain?>(null);
    }

    public async Task GetTenantsAsync()
    {
        _logger.LogDebug("Retrieved all tenants");
    }

    public async ValueTask<ITenantGrain?> CreateTenantAsync(string id, Dictionary<string, string?>? metadata = null)
    {
        id.ThrowIfNull().IfWhiteSpace();

        _registrations.State.Add(id);
        await _registrations.WriteStateAsync();

        var grain = _grainFactory.GetGrain<ITenantGrain>(id);
        
        _logger.LogInformation("Created tenant with ID {Id}", id);
        return grain;
    }

    public async Task RemoveTenantAsync(string id)
    {
        id.ThrowIfNull().IfEmpty();

        // TODO: Perform tenant removal.
        
        _logger.LogInformation("Removed tenant with ID {Id}", id);
    }
}