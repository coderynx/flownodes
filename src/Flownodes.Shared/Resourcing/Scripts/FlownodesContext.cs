using Flownodes.Sdk.Alerting;
using Flownodes.Sdk.Entities;
using Flownodes.Shared.Alerting.Grains;
using Flownodes.Shared.Resourcing.Exceptions;
using Flownodes.Shared.Resourcing.Grains;
using Microsoft.Extensions.Logging;

namespace Flownodes.Shared.Resourcing.Scripts;

public class FlownodesContext
{
    private readonly IAlertManagerGrain _alertManager;

    private readonly ILogger<FlownodesContext> _logger;
    private readonly IResourceManagerGrain _resourceManager;

    public FlownodesContext(ILogger<FlownodesContext> logger, IResourceManagerGrain resourceManager,
        IAlertManagerGrain alertManager, FlownodesId id)
    {
        _logger = logger;
        _resourceManager = resourceManager;
        _alertManager = alertManager;
        Id = id;
    }

    public FlownodesId Id { get; }
    public string TenantName => Id.FirstName;
    public string ResourceName => Id.SecondName!;

    public void LogInformation(string? message)
    {
        _logger.LogInformation("Message from script {@ResourceName} of tenant {@TenantName}: {@Message}", Id.FirstName,
            Id.SecondName, message);
    }

    public void LogWarning(string? message)
    {
        _logger.LogWarning("Message from script {@ResourceName} of tenant {@TenantName}: {@Message}", ResourceName,
            TenantName, message);
    }

    public void LogError(string? message)
    {
        _logger.LogError("Message from script {@ResourceName} of tenant {@TenantName}: {@Message}", ResourceName,
            TenantName, message);
    }

    public async Task<Dictionary<string, object?>> GetResourceState(
        string resourceName)
    {
        var resource = await _resourceManager.GetResourceAsync(resourceName);
        return await resource.AsReference<IStatefulResourceGrain>().GetState();
    }

    public async Task UpdateResourceStateAsync(string resourceName, Dictionary<string, object?> state)
    {
        var resource = await _resourceManager.GetResourceAsync(resourceName);
        await resource.AsReference<IStatefulResourceGrain>().UpdateStateAsync(state);
    }

    public async ValueTask<string> GetDataFromDataSourceAsync(string dataSourceName, string actionId,
        Dictionary<string, object?>? parameters = null)
    {
        var dataSource = await _resourceManager.GetResourceAsync<IDataSourceGrain>(dataSourceName);
        if (dataSource is null) throw new ResourceNotFoundException(TenantName, dataSourceName);

        var data = await dataSource.GetDataAsync(actionId, parameters);
        return data.JsonString;
    }

    public async Task CreateAlertAsync(AlertSeverity severity, string description, ISet<string> drivers)
    {
        var alert = await _alertManager.CreateAlertAsync(TenantName, ResourceName, severity, description, drivers);
        await alert.FireAsync();
    }
}