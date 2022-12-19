using Autofac;
using Autofac.Extensions.DependencyInjection;
using Flownodes.Core.Interfaces;
using Flownodes.Core.Models;
using Flownodes.Worker.Models;
using Orleans.Runtime;
using Throw;

namespace Flownodes.Worker.Implementations;

public class AlertManagerGrain : Grain, IAlerManagerGrain
{
    private readonly List<IAlerterDriver> _alerterDrivers = new();

    private readonly ILogger<AlertManagerGrain> _logger;
    private readonly IPersistentState<AlerterPersistence> _persistence;
    private readonly IServiceProvider _serviceProvider;

    public AlertManagerGrain(ILogger<AlertManagerGrain> logger,
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

    public async ValueTask<Alert> ProduceInfoAlertAsync(string frn, string message)
    {
        return await ProduceAlertAsync(frn, AlertKind.Info, message);
    }

    public async ValueTask<Alert> ProduceWarningAlertAsync(string frn, string message)
    {
        return await ProduceAlertAsync(frn, AlertKind.Warning, message);
    }

    public async ValueTask<Alert> ProduceErrorAlertAsync(string frn, string message)
    {
        return await ProduceAlertAsync(frn, AlertKind.Error, message);
    }

    public ValueTask<IEnumerable<Alert>> GetAlerts()
    {
        return ValueTask.FromResult<IEnumerable<Alert>>(_persistence.State.Alerts);
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

    private async ValueTask<Alert> ProduceAlertAsync(string frn, AlertKind kind, string message)
    {
        frn.ThrowIfNull().IfWhiteSpace();
        message.ThrowIfNull().IfWhiteSpace();

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