using Flownodes.Sdk.Alerting;

namespace Flownodes.Shared.Alerting;

public interface IAlertManagerGrain : IEntityGrain
{
    ValueTask<IAlertGrain> CreateAlertAsync(string targetObjectName, AlertSeverity severity, string description,
        ISet<string> driverIds);

    ValueTask<IAlertGrain> CreateAlertAsync(string alertName, string targetObjectName, AlertSeverity severity,
        string description, ISet<string> driverIds);

    ValueTask<IAlertGrain?> GetAlert(string alertName);
    ValueTask<IAlertGrain?> GetAlertByTargetObjectName(string targetObjectName);
    Task RemoveAlertAsync(string alertName);
    ValueTask<HashSet<IAlertGrain>> GetAlerts();
}