using Flownodes.Worker.Extensions;

namespace Flownodes.Worker.Models;

[GenerateSerializer]
internal sealed class ResourceMetadataStore
{
    [Id(0)] public Dictionary<string, string?> Properties { get; set; } = new();

    [Id(1)] public DateTime CreatedAt { get; } = DateTime.Now;

    public void UpdateProperties(Dictionary<string, string?>? properties)
    {
        if (properties is null)
        {
            Properties.Clear();
            return;
        }

        Properties.MergeInPlace(properties);
    }
}