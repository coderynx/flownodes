namespace Flownodes.Shared.Alerting.Exceptions;

[GenerateSerializer]
public class InvalidAlertException : Exception
{
    public InvalidAlertException(string tenantName, string alertName) : base(
        $"The alert {alertName} of tenant {tenantName} is invalid")
    {
        TenantName = tenantName;
        AlertName = alertName;
    }

    [Id(0)] public string TenantName { get; }

    [Id(1)] public string AlertName { get; }
}