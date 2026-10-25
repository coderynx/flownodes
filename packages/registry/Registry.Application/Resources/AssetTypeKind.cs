using System.Text.Json;
using Registry.Domain.Resources;

namespace Registry.Application.Resources;

internal sealed class AssetTypeKind : IResourceKind
{
    public const string KindName = "AssetType";
    public string Name => KindName;
    public string Plural => "assettypes";

    public IEnumerable<SpecError> Validate(JsonElement spec, NamespaceSnapshot @namespace)
    {
        foreach (var field in spec.EnumerateObject().Where(field => field.Name != "dataPoints"))
        {
            yield return new SpecError($"spec.{field.Name}", "Unknown field.");
        }

        foreach (var error in DataPointDeclarations.Validate(spec))
        {
            yield return error;
        }
    }

    public IEnumerable<SpecReference> References(JsonElement spec) => [];
}
