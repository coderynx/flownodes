namespace Flownodes.Shared.Resourcing.Exceptions;

[GenerateSerializer]
public class ResourceNotConfigurableException : Exception
{
    public ResourceNotConfigurableException(string tenantName, string kind, string resourceName) : base(
        $"The resource {resourceName} of tenant {tenantName} is not configurable")
    {
        TenantName = tenantName;
        Kind = kind;
        ResourceName = resourceName;
    }

    [Id(0)] public string TenantName { get; }

    [Id(1)] public string Kind { get; }

    [Id(2)] public string ResourceName { get; }
}