using Flownodes.Sdk.Alerting;

namespace Flownodes.Shared.Interfaces;

public interface IAlertManagerGrain : IGrainWithStringKey
{
    ValueTask<IAlertGrain> CreateAlertAsync(string tenantName, string targetObjectName,
        AlertSeverity severity, string description, ISet<string> driverIds);

    ValueTask<IAlertGrain> CreateAlertAsync(string tenantName, string alertName, string targetObjectName,
        AlertSeverity severity, string description, ISet<string> driverIds);

    ValueTask<IAlertGrain?> GetAlert(string tenantName, string alertName);
    ValueTask<IAlertGrain?> GetAlertByTargetObjectName(string tenantName, string targetObjectName);
    Task RemoveAlertAsync(string tenantName, string alertName);
    ValueTask<HashSet<string>> GetAlertNames(string tenantName);
    ValueTask<HashSet<IAlertGrain>> GetAlerts(string tenantName);
}