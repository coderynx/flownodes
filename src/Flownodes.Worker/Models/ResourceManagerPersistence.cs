using Orleans.Runtime;

namespace Flownodes.Worker.Models;

[GenerateSerializer]
public record ResourceRegistration([property: Id(0)] string ResourceId, [property: Id(1)] GrainId GrainId,
    [property: Id(2)] string Kind,
    [property: Id(3)] string Frn);

[GenerateSerializer]
public class ResourceManagerPersistence
{
    [Id(0)] public List<ResourceRegistration> Registrations { get; set; } = new();

    public void AddRegistration(string resourceId, GrainId grainId, string kind, string frn)
    {
        Registrations.Add(new ResourceRegistration(resourceId, grainId, kind, frn));
    }
}