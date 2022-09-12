namespace Flownodes.Cluster.Node.Models;

public class ResourcePersistence
{
    public string BehaviorId { get; set; }
    public DateTime? CreatedAt { get; set; } = DateTime.Now;
    public Dictionary<string, object?> Configuration { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
    public ResourceState State { get; set; } = new();
}