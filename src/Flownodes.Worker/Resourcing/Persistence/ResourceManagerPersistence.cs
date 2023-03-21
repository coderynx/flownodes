using Flownodes.Shared.Resourcing.Attributes;
using Flownodes.Shared.Resourcing.Grains;
using Orleans.Runtime;

namespace Flownodes.Worker.Resourcing.Persistence;

[GenerateSerializer]
public sealed class ResourceRegistration
{
    public ResourceRegistration(string resourceName, GrainId grainId, string kind, HashSet<string>? tags = null)
    {
        ResourceName = resourceName;
        GrainId = grainId;
        Kind = kind;

        if (tags is not null) Tags = tags;
    }

    [Id(0)] public string ResourceName { get; set; }
    [Id(1)] public GrainId GrainId { get; set; }
    [Id(2)] public string Kind { get; set; }
    [Id(3)] public HashSet<string> Tags { get; } = new();
}

[GenerateSerializer]
public sealed class ResourceManagerPersistence
{
    [Id(0)] public List<ResourceRegistration> Registrations { get; set; } = new();

    public void AddRegistration(string resourceName, GrainId grainId, string kind, HashSet<string>? tags = null)
    {
        Registrations.Add(new ResourceRegistration(resourceName, grainId, kind, tags));
    }

    public ResourceRegistration? GetRegistration(string resourceName)
    {
        return Registrations
            .FirstOrDefault(x => x.ResourceName.Equals(resourceName));
    }

    public bool IsSingletonResourceRegistered<TResourceGrain>(string kind) where TResourceGrain : IResourceGrain
    {
        return Attribute.IsDefined(typeof(TResourceGrain), typeof(SingletonResourceAttribute))
               && IsKindRegistered(kind);
    }

    public bool IsResourceRegistered(string resourceName)
    {
        return Registrations
            .Any(x => x.ResourceName.Equals(resourceName));
    }

    public bool IsKindRegistered(string kind)
    {
        return Registrations.Any(x => x.Kind.Equals(kind));
    }
}