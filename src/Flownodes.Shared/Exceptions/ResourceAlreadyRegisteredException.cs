namespace Flownodes.Shared.Exceptions;

public class ResourceAlreadyRegisteredException : Exception
{
    public ResourceAlreadyRegisteredException(string tenantNane, string resourceName) : base(
        $"The resource {resourceName} of tenant {tenantNane} is already registered")
    {
    }
}