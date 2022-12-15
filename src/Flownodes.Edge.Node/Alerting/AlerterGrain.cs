using Autofac;
using Autofac.Extensions.DependencyInjection;
using Flownodes.Edge.Core.Alerting;
using Flownodes.Edge.Node.Models;
using Orleans.Runtime;

namespace Flownodes.Edge.Node.Alerting;

public class AlerterGrain : Grain, IAlerterGrain
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

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Alerter activated");
        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Alerter deactivated");
        return base.OnDeactivateAsync(reason, cancellationToken);
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