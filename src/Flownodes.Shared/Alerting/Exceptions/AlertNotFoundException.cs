namespace Flownodes.Shared.Alerting.Exceptions;

public class AlertNotFoundException : Exception
{
    public AlertNotFoundException(string tenantName, string alertId) : base(
        $"Alert {alertId} of tenant {tenantName} could not be found")
    {
        TenantName = tenantName;
        AlertId = alertId;
    }

    public string TenantName { get; }
    public string AlertId { get; }
}