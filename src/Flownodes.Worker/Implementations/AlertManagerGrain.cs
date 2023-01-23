using Autofac;
using Autofac.Extensions.DependencyInjection;
using Flownodes.Sdk.Alerting;
using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;
using MapsterMapper;
using Orleans.Runtime;
using Throw;

namespace Flownodes.Worker.Implementations;

[GenerateSerializer]
public sealed record AlertManagerPersistence
{
    [Id(0)] public List<Alert> Registrations { get; set; } = new();
    [Id(1)] public List<string> DriversNames { get; set; } = new();
}

public class AlertManagerGrain : Grain, IAlertManagerGrain
{
    private readonly List<IAlerterDriver> _drivers = new();
    private readonly ILogger<AlertManagerGrain> _logger;
    private readonly IMapper _mapper;
    private readonly IPersistentState<AlertManagerPersistence> _persistence;
    private readonly IServiceProvider _serviceProvider;

    public AlertManagerGrain(ILogger<AlertManagerGrain> logger,
        [PersistentState("alertManagerStore", "flownodes")]
        IPersistentState<AlertManagerPersistence> persistence,
        IServiceProvider serviceProvider, IMapper mapper)
    {
        _logger = logger;
        _persistence = persistence;
        _serviceProvider = serviceProvider;
        _mapper = mapper;
    }

    private string Id => this.GetPrimaryKeyString();

    public async Task SetupAsync(params string[] alertDriverIds)
    {
        if (alertDriverIds.Length > 0)
        {
            foreach (var alertDriverId in alertDriverIds)
            {
                var alertDriver = _serviceProvider.GetAutofacRoot().ResolveKeyed<IAlerterDriver>(alertDriverId);
                _drivers.Add(alertDriver);
                _persistence.State.DriversNames.Add(alertDriverId);
                _logger.LogInformation("Loaded alerter driver {AlerterDriverId}", alertDriverId);
            }

            await _persistence.WriteStateAsync();
        }

        _logger.LogInformation("Alerter configured");
    }

    public async ValueTask<Alert> FireInfoAsync(string targetResourceId, string description)
    {
        return await FireAlertAsync(targetResourceId, AlertSeverity.Informational, description);
    }

    public async ValueTask<Alert> FireWarningAsync(string targetResourceId, string description)
    {
        return await FireAlertAsync(targetResourceId, AlertSeverity.Warning, description);
    }

    public async ValueTask<Alert> FireErrorAsync(string targetResourceId, string description)
    {
        return await FireAlertAsync(targetResourceId, AlertSeverity.Error, description);
    }

    public ValueTask<IEnumerable<Alert>> GetAlerts()
    {
        return ValueTask.FromResult<IEnumerable<Alert>>(_persistence.State.Registrations);
    }

    public async Task ClearAlertsAsync()
    {
        _persistence.State.Registrations.Clear();
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Cleared stored alerts of tenant {TenantId}", Id);
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Alert manager of tenant {TenantId} activated", Id);
        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Alert manager of tenant {TenantId} deactivated", Id);
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    private void LoadDrivers()
    {
        if (_drivers.Count > 0) return;

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var alertDriverId in _persistence.State.DriversNames)
        {
            var alertDriver = _serviceProvider.GetAutofacRoot().ResolveKeyed<IAlerterDriver>(alertDriverId);
            _drivers.Add(alertDriver);
        }

        _logger.LogInformation("Drivers loaded");
    }

    private async ValueTask<Alert> FireAlertAsync(string targetResourceId, AlertSeverity severity,
        string description)
    {
        targetResourceId.ThrowIfNull().IfWhiteSpace();
        description.ThrowIfNull().IfWhiteSpace();

        LoadDrivers();

        var alert = new Alert(Guid.NewGuid(), targetResourceId, severity, DateTime.Now, description);

        var alertToFire = _mapper.Map<AlertToFire>(alert);
        foreach (var driver in _drivers) await driver.SendAlertAsync(alertToFire);

        _persistence.State.Registrations.Add(alert);
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Dispatched alert {AlertId} to all drivers", alert.Id);

        return alert;
    }
}