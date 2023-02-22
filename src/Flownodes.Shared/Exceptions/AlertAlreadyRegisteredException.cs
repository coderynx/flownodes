namespace Flownodes.Shared.Exceptions;

public class AlertAlreadyRegisteredException : Exception
{
    public AlertAlreadyRegisteredException(string tenantName, string alertName) : base(
        $"Alert {alertName} of tenant {tenantName} already registered")
    {
        TenantName = tenantName;
        AlertName = alertName;
    }

    public string TenantName { get; }
    public string AlertName { get; }
}