namespace Flownodes.Shared.Exceptions;

public class TenantAlreadyRegisteredException : Exception
{
    public TenantAlreadyRegisteredException(string tenantName) : base($"The tenant {tenantName} is already registered")
    {
    }
}