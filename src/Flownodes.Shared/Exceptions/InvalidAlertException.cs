namespace Flownodes.Shared.Exceptions;

public class InvalidAlertException : Exception
{
    public InvalidAlertException(string tenantName, string alertName) : base(
        $"The alert {alertName} of tenant {tenantName} is invalid")
    {
        TenantName = tenantName;
        AlertName = alertName;
    }

    public string TenantName { get; set; }
    public string AlertName { get; set; }
}