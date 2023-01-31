using Orleans.Runtime;

namespace Flownodes.Worker.Models;

[GenerateSerializer]
public sealed class ResourceRegistration
{
    public ResourceRegistration(string ResourceId, GrainId GrainId, string Kind)
    {
        this.ResourceId = ResourceId;
        this.GrainId = GrainId;
        this.Kind = Kind;
    }

    [Id(0)] public string ResourceId { get; init; }
    [Id(1)] public GrainId GrainId { get; init; }
    [Id(2)] public string Kind { get; init; }
}

[GenerateSerializer]
public sealed class ResourceManagerPersistence
{
    [Id(0)] public List<ResourceRegistration> Registrations { get; set; } = new();

    public void AddRegistration(string resourceId, GrainId grainId, string kind)
    {
        Registrations.Add(new ResourceRegistration(resourceId, grainId, kind));
    }

    public bool IsKindRegistered(string kind)
    {
        return Registrations.Any(x => x.Kind.Equals(kind));
    }
}