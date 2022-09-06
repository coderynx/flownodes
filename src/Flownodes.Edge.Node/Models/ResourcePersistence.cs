namespace Flownodes.Edge.Node.Models;

public class ResourcePersistence
{
    public string BehaviorId { get; set; }
    public DateTime? CreatedAt { get; set; }
    public Dictionary<string, object?> Configuration { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public ResourceState State { get; set; }
}