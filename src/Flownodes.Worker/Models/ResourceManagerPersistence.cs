using Orleans.Runtime;

namespace Flownodes.Worker.Models;

[GenerateSerializer]
public record ResourceRegistration([property: Id(0)] GrainId Id, [property: Id(1)] string Kind,
    [property: Id(2)] string Frn);

[GenerateSerializer]
public class ResourceManagerPersistence
{
    [Id(0)] public List<ResourceRegistration> Registrations { get; set; } = new();

    public void AddRegistration(GrainId id, string kind, string frn)
    {
        Registrations.Add(new ResourceRegistration(id, kind, frn));
    }
}