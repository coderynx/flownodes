using Flownodes.Sdk.Entities;

namespace Flownodes.Shared.Resourcing.Exceptions;

[GenerateSerializer]
public class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException(EntityId id)
        : base($"The resource {id.SecondName} of tenant {id.FirstName} was not found")
    {
        TenantName = id.FirstName;
        ResourceName = id.SecondName!;
    }

    public ResourceNotFoundException(string tenantName, string resourceName) :
        base($"The resource {resourceName} of tenant {tenantName} was not found")
    {
        TenantName = tenantName;
        ResourceName = resourceName;
    }

    [Id(0)] public string TenantName { get; }

    [Id(1)] public string ResourceName { get; }
}