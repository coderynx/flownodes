using Flownodes.Sdk.Alerting;
using Flownodes.Sdk.Entities;
using Flownodes.Shared.Alerting.Exceptions;
using Flownodes.Shared.Alerting.Grains;
using Flownodes.Worker.Alerting.Persistence;
using Flownodes.Worker.Extendability;
using Orleans.Runtime;

namespace Flownodes.Worker.Alerting;

[GrainType(EntityNames.Alert)]
internal class AlertGrain : Grain, IAlertGrain
{
    private readonly IList<IAlerterDriver> _drivers = new List<IAlerterDriver>();
    private readonly IExtensionProvider _extensionProvider;
    private readonly ILogger<AlertGrain> _logger;
    private readonly IPersistentState<AlertPersistence> _persistence;

    public AlertGrain(ILogger<AlertGrain> logger,
        IExtensionProvider extensionProvider,
        [PersistentState("alertPersistence")] IPersistentState<AlertPersistence> persistence)
    {
        _logger = logger;
        _persistence = persistence;
        _extensionProvider = extensionProvider;
    }

    private EntityId Id => (EntityId)this.GetPrimaryKeyString();
    private string TenantName => Id.FirstName;
    private string AlertName => Id.SecondName!;

    private AlertPersistence State => _persistence.State;

    public async Task InitializeAsync(string targetObjectName, DateTime firedAt, AlertSeverity severity,
        string description, ISet<string> drivers)
    {
        ArgumentException.ThrowIfNullOrEmpty(targetObjectName);
        ArgumentException.ThrowIfNullOrEmpty(description);

        _persistence.State.TargetObjectName = targetObjectName;
        _persistence.State.FiredAt = firedAt;
        _persistence.State.Severity = severity;
        _persistence.State.Description = description;
        _persistence.State.DriverIds = drivers;
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Initialized alert grain {@AlertId}", Id);
    }

    public ValueTask<string> GetName()
    {
        return ValueTask.FromResult(AlertName);
    }

    public ValueTask<(string TargetObjectName, DateTime FiredAt, AlertSeverity Severity, string Description,
        ISet<string> Drivers)> GetState()
    {
        if (State.TargetObjectName is null || State.Description is null)
            throw new InvalidAlertException(TenantName, AlertName);

        var state = (State.TargetObjectName, State.FiredAt, State.Severity, State.Description, State.DriverIds);

        _logger.LogDebug("Retrieved state of alert {@AlertId}", Id);
        return ValueTask.FromResult(state);
    }

    public async Task FireAsync()
    {
        if (_drivers.Count is 0)
            LoadDrivers();

        if (State.TargetObjectName is null || State.Description is null)
            throw new InvalidAlertException(TenantName, AlertName);

        await SendToDriversAsync(State.TargetObjectName, State.FiredAt, State.Severity, State.Description);
    }

    public async Task ClearStateAsync()
    {
        await _persistence.ClearStateAsync();
        _logger.LogInformation("Cleared state of alert {@AlertGrainId}", Id);
    }

    public ValueTask<EntityId> GetId()
    {
        return ValueTask.FromResult(Id);
    }

    private void LoadDrivers()
    {
        foreach (var alertDriverId in _persistence.State.DriverIds)
        {
            var alertDriver = _extensionProvider.GetAlerterDriver(alertDriverId);
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