using Flownodes.Sdk.Alerting;
using Flownodes.Shared.Exceptions;
using Flownodes.Shared.Interfaces;
using Flownodes.Worker.Services;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

[GenerateSerializer]
internal sealed record AlertPersistence
{
    [Id(0)] public string? TargetObjectName { get; set; }

    [Id(1)] public DateTime FiredAt { get; set; }

    [Id(2)] public AlertSeverity Severity { get; set; }

    [Id(3)] public string? Description { get; set; }
    [Id(4)] public ISet<string> AlerterDrivers { get; set; } = new HashSet<string>();
}

internal class AlertGrain : Grain, IAlertGrain
{
    private readonly IList<IAlerterDriver> _drivers = new List<IAlerterDriver>();

    private readonly ILogger<AlertGrain> _logger;
    private readonly IPersistentState<AlertPersistence> _persistence;
    private readonly IPluginProvider _pluginProvider;

    public AlertGrain(ILogger<AlertGrain> logger,
        IPluginProvider pluginProvider,
        [PersistentState("alertPersistence")] IPersistentState<AlertPersistence> persistence)
    {
        _logger = logger;
        _persistence = persistence;
        _pluginProvider = pluginProvider;
    }

    private string Id => this.GetPrimaryKeyString();
    private string TenantName => Id.Split('/')[0];
    private string AlertName => Id.Split('/')[1];

    public async Task InitializeAsync(string targetObjectName, DateTime firedAt, AlertSeverity severity,
        string description, ISet<string> alertDrivers)
    {
        _persistence.State.TargetObjectName = targetObjectName;
        _persistence.State.FiredAt = firedAt;
        _persistence.State.Severity = severity;
        _persistence.State.Description = description;
        _persistence.State.AlerterDrivers = alertDrivers;
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Initialized alert grain {@AlertId}", Id);
    }

    public async Task FireAsync()
    {
        LoadDrivers();

        if (_persistence.State.TargetObjectName is null || _persistence.State.Description is null)
            throw new InvalidAlertException(TenantName, AlertName);

        await SendToDriversAsync(_persistence.State.TargetObjectName, _persistence.State.FiredAt,
            _persistence.State.Severity, _persistence.State.Description);
    }

    public async Task ClearStateAsync()
    {
        await _persistence.ClearStateAsync();
        _logger.LogInformation("Cleared state of alert {@AlertGrainId}", Id);
    }

    private void LoadDrivers()
    {
        foreach (var alertDriverId in _persistence.State.AlerterDrivers)
        {
            var alertDriver = _pluginProvider.GetAlerterDriver(alertDriverId);
            if (alertDriver is null)
            {
                _logger.LogError("Alerter driver {@AlerterDriverId} not found, skipped", alertDriverId);
                continue;
            }

            _drivers.Add(alertDriver);
            _logger.LogInformation("Loaded alerter driver {@AlerterDriverId}", alertDriverId);
        }
    }

    private async Task SendToDriversAsync(string targetObjectName, DateTime firedAt, AlertSeverity severity,
        string description)
    {
        foreach (var driver in _drivers)
        {
            var alertToFire = new AlertToFire(Id, firedAt, severity, targetObjectName, description);
            await driver.SendAlertAsync(alertToFire);
            _logger.LogInformation("Fired alert {@Alert}", alertToFire);
        }
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activated alert grain {@AlertGrainId}", Id);
        return Task.CompletedTask;
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivated alert grain {@AlertGrainId} for reason {@DeactivationReason}", Id,
            reason.Description);
        return Task.CompletedTask;
    }
}