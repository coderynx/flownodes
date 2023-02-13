using Orleans.Runtime;

namespace Flownodes.Worker.Models;

[GenerateSerializer]
public sealed class ResourceRegistration
{
    public ResourceRegistration(string tenantName, string resourceName, GrainId grainId, string kind)
    {
        TenantName = tenantName;
        ResourceName = resourceName;
        GrainId = grainId;
        Kind = kind;
    }

    [Id(0)] public string TenantName { get; set; }
    [Id(1)] public string ResourceName { get; set; }
    [Id(2)] public GrainId GrainId { get; set; }
    [Id(3)] public string Kind { get; set; }
}

[GenerateSerializer]
public sealed class ResourceManagerPersistence
{
    [Id(0)] public List<ResourceRegistration> Registrations { get; set; } = new();

    public void AddRegistration(string tenantId, string resourceId, GrainId grainId, string kind)
    {
        Registrations.Add(new ResourceRegistration(tenantId, resourceId, grainId, kind));
    }

    public bool IsKindRegistered(string kind)
    {
        return Registrations.Any(x => x.Kind.Equals(kind));
    }
}