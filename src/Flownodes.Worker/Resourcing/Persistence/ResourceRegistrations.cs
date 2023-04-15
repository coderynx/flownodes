
using Flownodes.Shared.Resourcing.Attributes;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Worker.Extensions;
using Orleans.Runtime;

namespace Flownodes.Worker.Resourcing.Persistence;

[GenerateSerializer]
public sealed record ResourceRegistration
{
    public ResourceRegistration(GrainId grainId, HashSet<string>? tags = null)
    {
        GrainId = grainId;

        if (tags is not null) Tags = tags;
    }
    
    [Id(0)] public GrainId GrainId { get; set; }
    [Id(1)] public HashSet<string> Tags { get; } = new();
}

[GenerateSerializer]
public sealed class ResourceRegistrations
{
    [Id(0)] public List<ResourceRegistration> Registrations { get; } = new();

    public void AddRegistration(GrainId grainId, HashSet<string>? tags = null)
    {
        Registrations.Add(new ResourceRegistration(grainId, tags));
    }

    public ResourceRegistration? GetRegistration(string name)
    {
        return Registrations
            .FirstOrDefault(x => x.GrainId.ToFlownodesId().SecondName!.Equals(name));
    }

    public bool IsSingletonResourceRegistered<TResourceGrain>(string kind) where TResourceGrain : IResourceGrain
    {
        return Attribute.IsDefined(typeof(TResourceGrain), typeof(SingletonResourceAttribute))
               && IsKindRegistered(kind);
    }

    public bool IsResourceRegistered(string resourceName)
    {
        return Registrations
            .Any(x => x.GrainId.ToFlownodesId().SecondName!.Equals(resourceName));
    }

    public bool IsKindRegistered(string kind)
    {
        return Registrations.Any(x => x.GrainId.ToFlownodesId().ToEntityKindString().Equals(kind));
    }
}