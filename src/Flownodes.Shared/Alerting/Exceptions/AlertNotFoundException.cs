namespace Flownodes.Shared.Alerting.Exceptions;

[GenerateSerializer]
public class AlertNotFoundException : Exception
{
    public AlertNotFoundException(string tenantName, string alertId) : base(
        $"Alert {alertId} of tenant {tenantName} could not be found")
    {
        TenantName = tenantName;
        AlertId = alertId;
    }

    [Id(0)] public string TenantName { get; }

    [Id(1)] public string AlertId { get; }
}