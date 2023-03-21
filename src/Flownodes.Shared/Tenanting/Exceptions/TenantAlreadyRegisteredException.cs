namespace Flownodes.Shared.Tenanting.Exceptions;

[GenerateSerializer]
public class TenantAlreadyRegisteredException : Exception
{
    public TenantAlreadyRegisteredException(string tenantName) : base($"The tenant {tenantName} is already registered")
    {
        TenantName = tenantName;
    }

    [Id(0)] public string TenantName { get; }
}