using Flownodes.Sdk.Alerting;
using Flownodes.Shared;
using Flownodes.Shared.Exceptions;
using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

[GenerateSerializer]
internal sealed record AlertRegistration([property: Id(0)] string TenantName, [property: Id(1)] string AlertName,
    [property: Id(2)] string TargetObjectName);

[GrainType(FlownodesObjectNames.AlertManagerName)]
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

    public async ValueTask<IAlertGrain> CreateAlertAsync(string tenantName, string targetObjectName,
        AlertSeverity severity, string description, ISet<string> driverIds)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantName);
        ArgumentException.ThrowIfNullOrEmpty(targetObjectName);

        var alertName = Guid.NewGuid().ToString();
        return await CreateAlertAsync(tenantName, alertName, targetObjectName, severity, description, driverIds);
    }

    public async ValueTask<IAlertGrain> CreateAlertAsync(string tenantName, string alertName, string targetObjectName,
        AlertSeverity severity,
        string description, ISet<string> driverIds)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantName);
        ArgumentException.ThrowIfNullOrEmpty(alertName);
        ArgumentException.ThrowIfNullOrEmpty(targetObjectName);

        if (_alertRegistrations.State.Any(x => x.TenantName.Equals(tenantName) && x.AlertName.Equals(alertName)))
            throw new AlertAlreadyRegisteredException(tenantName, alertName);

        var id = new FlownodesId(FlownodesObject.Alert, tenantName, alertName);
        var grain = _grainFactory.GetGrain<IAlertGrain>(id);
        await grain.InitializeAsync(targetObjectName, DateTime.Now, severity, description, driverIds);

        var registration = await AddRegistrationAsync(tenantName, alertName, targetObjectName);

        _logger.LogInformation("Registered alert {@AlertRegistration}", registration);

        return grain;
    }

    public ValueTask<IAlertGrain?> GetAlertByTargetObjectName(string tenantName, string targetObjectName)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantName);
        ArgumentException.ThrowIfNullOrEmpty(targetObjectName);

        var registration = _alertRegistrations.State
            .FirstOrDefault(x => x.TenantName.Equals(tenantName) && x.TargetObjectName.Equals(targetObjectName));

        if (registration is null) return default;

        var id = new FlownodesId(FlownodesObject.Alert, registration.TenantName, registration.AlertName);
        return ValueTask.FromResult<IAlertGrain?>(_grainFactory.GetGrain<IAlertGrain>(id));
    }

    public ValueTask<IAlertGrain?> GetAlert(string tenantName, string alertName)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantName);
        ArgumentException.ThrowIfNullOrEmpty(alertName);

        if (!_alertRegistrations.State.Any(x => x.TenantName.Equals(tenantName) && x.AlertName.Equals(alertName)))
            return default;

        var id = $"{tenantName}/{alertName}";
        var grain = _grainFactory.GetGrain<IAlertGrain>(id);

        _logger.LogDebug("Retrieved alert grain {@AlertGrainId}", id);
        return ValueTask.FromResult<IAlertGrain?>(grain);
    }

    public ValueTask<HashSet<string>> GetAlertNames(string tenantName)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantName);

        var names = _alertRegistrations.State
            .Where(x => x.TenantName.Equals(tenantName))
            .Select(x => x.AlertName)
            .ToHashSet();

        return ValueTask.FromResult(names);
    }

    public ValueTask<HashSet<IAlertGrain>> GetAlerts(string tenantName)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantName);

        var alerts = _alertRegistrations.State
            .Where(x => x.TenantName.Equals(tenantName))
            .Select(x => _grainFactory.GetGrain<IAlertGrain>($"{x.TenantName}/{x.AlertName}"))
            .ToHashSet();

        return ValueTask.FromResult(alerts);
    }

    public async Task RemoveAlertAsync(string tenantName, string alertName)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantName);
        ArgumentException.ThrowIfNullOrEmpty(alertName);

        var registration = _alertRegistrations.State
            .SingleOrDefault(x => x.TenantName.Equals(tenantName) && x.AlertName.Equals(alertName));

        if (registration is null) throw new AlertNotFoundException(tenantName, alertName);

        var grain = _grainFactory.GetGrain<IAlertGrain>(alertName);
        await grain.ClearStateAsync();
        _alertRegistrations.State.Remove(registration);

        _logger.LogInformation("Removed alert {@AlertRegistration}", registration);
    }

    private async ValueTask<AlertRegistration> AddRegistrationAsync(string tenantName, string alertName,
        string targetObjectName)
    {
        ArgumentException.ThrowIfNullOrEmpty(tenantName);
        ArgumentException.ThrowIfNullOrEmpty(alertName);

        var registration = new AlertRegistration(tenantName, alertName, targetObjectName);
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