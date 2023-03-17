namespace Flownodes.Shared.Tenanting.Exceptions;

public class TenantAlreadyRegisteredException : Exception
{
    public TenantAlreadyRegisteredException(string tenantName) : base($"The tenant {tenantName} is already registered")
    {
        TenantName = tenantName;
    }

    public string TenantName { get; }
}