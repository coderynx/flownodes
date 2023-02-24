using Flownodes.Worker.Extensions;

namespace Flownodes.Worker.Models;

[GenerateSerializer]
internal sealed class ResourceStateStore
{
    [Id(1)] public Dictionary<string, object?> Properties = new();
    [Id(0)] public DateTime LastUpdate { get; set; } = DateTime.Now;

    public void UpdateState(Dictionary<string, object?> properties)
    {
        Properties.MergeInPlace(properties);
        LastUpdate = DateTime.Now;
    }
}