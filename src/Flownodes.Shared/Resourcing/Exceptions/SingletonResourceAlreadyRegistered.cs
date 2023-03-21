namespace Flownodes.Shared.Resourcing.Exceptions;

[GenerateSerializer]
public class SingletonResourceAlreadyRegistered : Exception
{
    public SingletonResourceAlreadyRegistered(string tenantName, string resourceName) :
        base($"The singleton resource {resourceName} of tenant {tenantName} is already registered")
    {
        TenantName = tenantName;
        ResourceName = resourceName;
    }

    [Id(0)] public string TenantName { get; }
    [Id(1)] public string ResourceName { get; }
}