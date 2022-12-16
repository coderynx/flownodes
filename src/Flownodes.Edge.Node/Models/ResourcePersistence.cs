using Flownodes.Edge.Core.Resources;

namespace Flownodes.Edge.Node.Models;

[GenerateSerializer]
public class ResourcePersistence
{
    [Id(0)] public string BehaviourId { get; set; }
    [Id(1)] public DateTime? CreatedAt { get; set; } = DateTime.Now;
    [Id(2)] public ResourceConfiguration Configuration { get; set; } = new();
    [Id(3)] public Dictionary<string, string> Metadata { get; set; } = new();
    [Id(4)] public ResourceState State { get; set; } = new();
}