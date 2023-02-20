namespace Flownodes.Shared.Exceptions;

public class SingletonResourceAlreadyRegistered : Exception
{
    public SingletonResourceAlreadyRegistered(string tenantName, string resourceName) :
        base($"The singleton resource {resourceName} of tenant {tenantName} is already registered")
    {

    }
}