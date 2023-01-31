using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

public class TenantGrain : Grain, ITenantGrain
{
    private readonly IPersistentState<TenantConfiguration> _configuration;
    private readonly IGrainFactory _grainFactory;

    private readonly ILogger<TenantGrain> _logger;

    public TenantGrain(ILogger<TenantGrain> logger,
        [PersistentState("tenantConfiguration")]
        IPersistentState<TenantConfiguration> configuration, IGrainFactory grainFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _grainFactory = grainFactory;
    }

    private string Id => this.GetPrimaryKeyString();
    private IResourceManagerGrain ResourceManagerGrain => _grainFactory.GetGrain<IResourceManagerGrain>(Id);
    private IAlertManagerGrain AlertManagerGrain => _grainFactory.GetGrain<IAlertManagerGrain>(Id);
    private IWorkflowManagerGrain WorkflowManagerGrain => _grainFactory.GetGrain<IWorkflowManagerGrain>(Id);

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

    public ValueTask<IAlertManagerGrain> GetAlertManager()
    {
        return ValueTask.FromResult(AlertManagerGrain);
    }

    public ValueTask<IWorkflowManagerGrain> GetWorkflowManager()
    {
        return ValueTask.FromResult(WorkflowManagerGrain);
    }
}