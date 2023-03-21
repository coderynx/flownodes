namespace Flownodes.Shared.Alerting.Exceptions;

[GenerateSerializer]
public class AlertAlreadyRegisteredException : Exception
{
    public AlertAlreadyRegisteredException(string tenantName, string alertName) : base(
        $"Alert {alertName} of tenant {tenantName} already registered")
    {
        TenantName = tenantName;
        AlertName = alertName;
    }

    [Id(0)] public string TenantName { get; }

    [Id(1)] public string AlertName { get; }
}