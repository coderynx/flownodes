using Flownodes.Sdk;
using Flownodes.Sdk.Alerting;
using Flownodes.Shared.Alerting;
using Flownodes.Shared.Alerting.Exceptions;
using Orleans.Runtime;

namespace Flownodes.Worker.Alerting;

[GenerateSerializer]
internal sealed record AlertRegistration([property: Id(0)] string AlertName, [property: Id(1)] string TargetObjectName);

[GrainType(FlownodesEntityNames.AlertManager)]
internal sealed class AlertManagerGrain : Grain, IAlertManagerGrain
{
    private readonly IPersistentState<HashSet<AlertRegistration>> _alertRegistrations;
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<AlertManagerGrain> _logger;

    public AlertManagerGrain(ILogger<AlertManagerGrain> logger, IGrainFactory grainFactory,
        [PersistentState("alertRegistrations")]
        IPersistentState<HashSet<AlertRegistration>> alertRegistrations)
    {
        _logger = logger;
        _grainFactory = grainFactory;
        _alertRegistrations = alertRegistrations;
    }

    private FlownodesId Id => (FlownodesId)this.GetPrimaryKeyString();
    private string TenantName => Id.FirstName;

    public async ValueTask<IAlertGrain> CreateAlertAsync(string targetObjectName, AlertSeverity severity,
        string description, ISet<string> driverIds)
    {
        ArgumentException.ThrowIfNullOrEmpty(targetObjectName);

        var alertName = Guid.NewGuid().ToString();
        return await CreateAlertAsync(alertName, targetObjectName, severity, description, driverIds);
    }

    public async ValueTask<IAlertGrain> CreateAlertAsync(string alertName, string targetObjectName,
        AlertSeverity severity,
        string description, ISet<string> driverIds)
    {
        ArgumentException.ThrowIfNullOrEmpty(alertName);
        ArgumentException.ThrowIfNullOrEmpty(targetObjectName);

        if (_alertRegistrations.State.Any(x => x.AlertName.Equals(alertName)))
            throw new AlertAlreadyRegisteredException(TenantName, alertName);

        var id = new FlownodesId(FlownodesEntity.Alert, TenantName, alertName);
        var grain = _grainFactory.GetGrain<IAlertGrain>(id);
        await grain.InitializeAsync(targetObjectName, DateTime.Now, severity, description, driverIds);

        var registration = await AddRegistrationAsync(alertName, targetObjectName);

        _logger.LogInformation("Registered alert {@AlertRegistration}", registration);

        return grain;
    }

    public ValueTask<IAlertGrain?> GetAlertByTargetObjectName(string targetObjectName)
    {
        ArgumentException.ThrowIfNullOrEmpty(targetObjectName);

        var registration = _alertRegistrations.State
            .FirstOrDefault(x => x.TargetObjectName.Equals(targetObjectName));

        if (registration is null) return default;

        var id = new FlownodesId(FlownodesEntity.Alert, registration.AlertName);
        return ValueTask.FromResult<IAlertGrain?>(_grainFactory.GetGrain<IAlertGrain>(id.IdString));
    }

    public ValueTask<IAlertGrain?> GetAlert(string alertName)
    {
        ArgumentException.ThrowIfNullOrEmpty(alertName);

        if (!_alertRegistrations.State.Any(x => x.AlertName.Equals(alertName)))
            return default;

        var id = new FlownodesId(FlownodesEntity.Alert, alertName);
        var grain = _grainFactory.GetGrain<IAlertGrain>(id);

        _logger.LogDebug("Retrieved alert grain {@AlertGrainId}", id);
        return ValueTask.FromResult<IAlertGrain?>(grain);
    }

    public ValueTask<HashSet<IAlertGrain>> GetAlerts()
    {
        var alerts = _alertRegistrations.State
            .Select(x =>
            {
                var id = new FlownodesId(FlownodesEntity.Alert, x.AlertName);
                return _grainFactory.GetGrain<IAlertGrain>(id);
            })
            .ToHashSet();

        return ValueTask.FromResult(alerts);
    }

    public async Task RemoveAlertAsync(string alertName)
    {
        ArgumentException.ThrowIfNullOrEmpty(alertName);

        var registration = _alertRegistrations.State
            .SingleOrDefault(x => x.AlertName.Equals(alertName));

        if (registration is null) throw new AlertNotFoundException(TenantName, alertName);

        var id = new FlownodesId(FlownodesEntity.Alert, alertName);
        var grain = _grainFactory.GetGrain<IAlertGrain>(id);
        await grain.ClearStateAsync();
        _alertRegistrations.State.Remove(registration);

        _logger.LogInformation("Removed alert {@AlertRegistration}", registration);
    }

    private async ValueTask<AlertRegistration> AddRegistrationAsync(string alertName, string targetObjectName)
    {
        ArgumentException.ThrowIfNullOrEmpty(alertName);

        var registration = new AlertRegistration(alertName, targetObjectName);
        _alertRegistrations.State.Add(registration);
        await _alertRegistrations.WriteStateAsync();
        return registration;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Alert manager activated");
        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Alert manager deactivated for reason {Reason}", reason.Description);
        return base.OnDeactivateAsync(reason, cancellationToken);
    }
}