using Flownodes.Worker.Extensions;
using Flownodes.Worker.Implementations;

namespace Flownodes.Worker.Models;

[GenerateSerializer]
internal sealed class ResourceStateStore
{
    [Id(1)] public Dictionary<string, object?> Properties { get; } = new();
    [Id(0)] public DateTime LastUpdate { get; private set; } = DateTime.Now;

    public void Update(Dictionary<string, object?> properties)
    {
        Properties.MergeInPlace(properties);
        LastUpdate = DateTime.Now;
    }
}