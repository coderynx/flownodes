using Newtonsoft.Json.Linq;

namespace Flownodes.Cluster.Node.Models;

public class AssetPersistence
{
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public AssetState State { get; set; } = new();
}

public class AssetState
{
    public DateTime? UpdatedAt { get; set; } = null;
    public JObject Data { get; set; } = JObject.FromObject(new { });
}