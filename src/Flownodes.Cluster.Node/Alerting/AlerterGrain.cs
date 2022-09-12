using System.Runtime.CompilerServices;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Flownodes.Cluster.Core.Alerting;
using Flownodes.Cluster.Node.Models;
using Orleans;
using Orleans.Runtime;

[assembly:
    InternalsVisibleTo(
        "DynamicProxyGenAssembly2, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113be11e6a7d3113e92484cf7045cc7")]

namespace Flownodes.Cluster.Node.Alerting;

internal class AlerterGrain : Grain, IAlerterGrain
{
    private readonly List<IAlerterDriver> _alerterDrivers = new();

    private readonly ILogger<AlerterGrain> _logger;
    private readonly IPersistentState<AlerterPersistence> _persistence;
    private readonly IServiceProvider _serviceProvider;

    public AlerterGrain(ILogger<AlerterGrain> logger,
        [PersistentState("alerterStore", "flownodes")]
        IPersistentState<AlerterPersistence> persistence,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _persistence = persistence;
        _serviceProvider = serviceProvider;
    }

    public async Task ConfigureAsync(params string[] alertDriverIds)
    {
        if (alertDriverIds.Length > 0)
        {
            foreach (var alertDriverId in alertDriverIds)
            {
                var alertDriver = _serviceProvider.GetAutofacRoot().ResolveKeyed<IAlerterDriver>(alertDriverId);

                _alerterDrivers.Add(alertDriver);
                _persistence.State.AlerterDrivers.Add(alertDriverId);
                _logger.LogInformation("Loaded alerter driver {AlerterDriverId}", alertDriverId);
            }

            await _persistence.WriteStateAsync();
        }

        _logger.LogInformation("Alerter configured");
    }

    public async Task<Alert> ProduceInfoAlertAsync(string frn, string message)
    {
        try
        {
            return await ProduceAlertAsync(frn, AlertKind.Info, message);
        }
        catch (Exception e)
        {
            _logger.LogError("Error producing alert: {ErrorMessage}", e.Message);
            throw;
        }
    }

    public async Task<Alert> ProduceWarningAlertAsync(string frn, string message)
    {
        try
        {
            return await ProduceAlertAsync(frn, AlertKind.Warning, message);
        }
        catch (Exception e)
        {
            _logger.LogError("Error producing alert: {ErrorMessage}", e.Message);
            throw;
        }
    }

    public async Task<Alert> ProduceErrorAlertAsync(string frn, string message)
    {
        try
        {
            return await ProduceAlertAsync(frn, AlertKind.Error, message);
        }
        catch (Exception e)
        {
            _logger.LogError("Error producing alert: {ErrorMessage}", e.Message);
            throw;
        }
    }

    public Task<IEnumerable<Alert>> GetAlerts()
    {
        return Task.FromResult<IEnumerable<Alert>>(_persistence.State.Alerts);
    }

    public async Task ClearAlertsAsync()
    {
        _persistence.State.Alerts.Clear();
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Cleared stored alerts");
    }

    public override Task OnActivateAsync()
    {
        _logger.LogInformation("Alerter activated");
        return base.OnActivateAsync();
    }

    public override Task OnDeactivateAsync()
    {
        _logger.LogInformation("Alerter deactivated");
        return base.OnDeactivateAsync();
    }

    private void LoadDrivers()
    {
        if (_alerterDrivers.Count > 0) return;

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var alertDriverId in _persistence.State.AlerterDrivers)
        {
            var alertDriver = _serviceProvider.GetAutofacRoot().ResolveKeyed<IAlerterDriver>(alertDriverId);
            _alerterDrivers.Add(alertDriver);
        }

        _logger.LogInformation("Drivers loaded");
    }

    private async Task<Alert> ProduceAlertAsync(string frn, AlertKind kind, string message)
    {
        LoadDrivers();

        var alert = new Alert(frn, kind, message);
        _persistence.State.Alerts.Add(alert);
        await _persistence.WriteStateAsync();
        _logger.LogInformation("Stored {AlertKind} alert of resource {Frn}", alert.Kind, alert.Frn);

        if (_persistence.State.Alerts.Count is 0) return alert;

        foreach (var alerterDriver in _alerterDrivers) await alerterDriver.SendAlertAsync(alert);
        _logger.LogInformation("Dispatched {AlertKind} alert of resource {Frn} to all drivers", alert.Kind, alert.Frn);

        return alert;
    }
}