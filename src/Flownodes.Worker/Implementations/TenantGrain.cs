using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

public class TenantGrain : Grain, ITenantGrain
{
    public TenantGrain(ILogger<TenantGrain> logger,
        [PersistentState("tenantConfiguration", "flownodes")]
        IPersistentState<TenantConfiguration> configuration, IGrainFactory grainFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _grainFactory = grainFactory;
    }

    private readonly ILogger<TenantGrain> _logger;
    private readonly IPersistentState<TenantConfiguration> _configuration;
    private readonly IGrainFactory _grainFactory;
    private string Id => this.GetPrimaryKeyString();
    private IResourceManagerGrain ResourceManagerGrain => _grainFactory.GetGrain<IResourceManagerGrain>(Id);

    public async Task UpdateConfigurationAsync(TenantConfiguration configuration)
    {
        _configuration.State = configuration;
        await _configuration.WriteStateAsync();
        
        _logger.LogInformation("Updated configuration for tenant {TenantId}", Id);
    }

    public ValueTask<TenantConfiguration> GetConfiguration()
    {
        _logger.LogDebug("Retrieved configuration for tenant {TenantId}", Id);
        return ValueTask.FromResult(_configuration.State);
    }

    public ValueTask<IResourceManagerGrain> GetResourceManager()
    {
        return ValueTask.FromResult(ResourceManagerGrain);
    }
}