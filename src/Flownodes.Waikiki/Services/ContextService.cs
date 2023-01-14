using Flownodes.Shared.Interfaces;

namespace Flownodes.Waikiki.Services;

public class ContextService : IContextService
{
    public ContextService(IGrainFactory grainFactory, ILogger<ContextService> logger)
    {
        _logger = logger;
        _tenantManagerGrain = grainFactory.GetGrain<ITenantManagerGrain>("tenant_manager");
    }
    
    public ITenantGrain? TenantGrain { get; private set; }
    private readonly ITenantManagerGrain _tenantManagerGrain;
    private readonly ILogger<ContextService> _logger;

    public async Task SetTenantAsync(string tenantName)
    {
        TenantGrain = await _tenantManagerGrain.GetTenantAsync(tenantName);
        _logger.LogInformation("Switched tenant to: {TenantName}", tenantName);
    }

    public async ValueTask<IResourceManagerGrain?> GetResourceManagerAsync()
    {
        return await TenantGrain.GetResourceManager();
    }
}