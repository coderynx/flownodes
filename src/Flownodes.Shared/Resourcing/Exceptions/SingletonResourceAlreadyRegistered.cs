namespace Flownodes.Shared.Resourcing.Exceptions;

public class SingletonResourceAlreadyRegistered : Exception
{
    public SingletonResourceAlreadyRegistered(string tenantName, string resourceName) :
        base($"The singleton resource {resourceName} of tenant {tenantName} is already registered")
    {
        TenantName = tenantName;
        ResourceName = resourceName;
    }

    public string TenantName { get; }
    public string ResourceName { get; }
}