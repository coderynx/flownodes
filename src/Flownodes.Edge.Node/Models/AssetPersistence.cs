using Newtonsoft.Json.Linq;

namespace Flownodes.Edge.Node.Models;

[GenerateSerializer]
public class AssetPersistence
{
    [Id(0)] public DateTime CreatedAt { get; set; } = DateTime.Now;
    [Id(1)] public AssetState State { get; set; } = new();
}

[GenerateSerializer]
public class AssetState
{
    [Id(0)] public DateTime? UpdatedAt { get; set; } = null;
    [Id(1)] public JObject Data { get; set; } = JObject.FromObject(new { });
}