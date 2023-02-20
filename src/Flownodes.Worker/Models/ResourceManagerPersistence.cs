using Flownodes.Shared.Attributes;
using Flownodes.Shared.Interfaces;
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

    public ResourceRegistration? GetRegistration(string tenantName, string resourceName)
    {
        return Registrations
            .FirstOrDefault(x => x.TenantName.Equals(tenantName) && x.ResourceName.Equals(resourceName));
    }

    public IEnumerable<ResourceRegistration> GetRegistrationsOfTenant(string tenantName)
    {
        return Registrations
            .Where(x => x.TenantName.Equals(tenantName))
            .ToList();
    }

    public bool IsSingletonResourceRegistered<TResourceGrain>(string kind) where TResourceGrain : IResourceGrain
    {
        return Attribute.IsDefined(typeof(TResourceGrain), typeof(SingletonResourceAttribute))
               && IsKindRegistered(kind);
    }

    public bool IsResourceRegistered(string tenantName, string resourceName)
    {
        return Registrations
            .Any(x => x.TenantName.Equals(tenantName) && x.ResourceName.Equals(resourceName));
    }

    public bool IsKindRegistered(string kind)
    {
        return Registrations.Any(x => x.Kind.Equals(kind));
    }
}