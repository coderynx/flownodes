using Flownodes.Core.Models;

namespace Flownodes.Worker.Models;

[GenerateSerializer]
public class ResourcePersistence
{
    [Id(0)] public string BehaviourId { get; set; }
    [Id(1)] public DateTime? CreatedAt { get; set; } = DateTime.Now;
    [Id(2)] public ResourceConfiguration Configuration { get; set; } = new();
    [Id(3)] public Dictionary<string, string> Metadata { get; set; } = new();
    [Id(4)] public ResourceState State { get; set; } = new();

    public void Setup(string behaviourId, ResourceConfiguration configuration, Dictionary<string, string> metadata)
    {
        BehaviourId = behaviourId;
        Configuration = configuration;
        Metadata = metadata;
    }
}