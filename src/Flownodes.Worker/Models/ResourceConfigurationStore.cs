using Flownodes.Worker.Extensions;

namespace Flownodes.Worker.Models;

[GenerateSerializer]
internal sealed class ResourceConfigurationStore
{
    [Id(0)] public Dictionary<string, object?> Properties { get; } = new();

    public void UpdateProperties(Dictionary<string, object?>? properties)
    {
        if (properties is null)
        {
            Properties.Clear();
            return;
        }

        Properties.MergeInPlace(properties);
    }
}