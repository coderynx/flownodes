namespace Flownodes.Shared.Tenanting.Exceptions;

public class TenantNotFoundException : Exception
{
    public TenantNotFoundException(string tenantName) : base($"Tenant {tenantName} not found")
    {
        TenantName = tenantName;
    }

    public string TenantName { get; }
}