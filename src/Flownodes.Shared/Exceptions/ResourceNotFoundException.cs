namespace Flownodes.Shared.Exceptions;

public class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException(string tenantName, string resourceName) :
        base($"The resource {resourceName} of tenant {tenantName} was not found")
    {
    }
}