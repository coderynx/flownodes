using System.Text.Json;
using Core.Application.Integrations;
using Registry.Domain.Resources;

namespace Registry.Application.Resources;

internal sealed class AssetKind(IIntegrationSchemas integrations) : IResourceKind
{
    private readonly AssetSources _sources = new(integrations);
    public const string KindName = "Asset";
    public string Name => KindName;
    public string Plural => "assets";

    public IEnumerable<SpecError> Validate(JsonElement spec, NamespaceSnapshot @namespace)
    {
        foreach (var field in spec.EnumerateObject()
                     .Where(field =>
                         field.Name is not ("assetType" or "parent" or "dataPoints" or "sources" or "alarms")))
        {
            yield return new SpecError($"spec.{field.Name}", "Unknown field.");
        }

        foreach (var name in new[] { "assetType", "parent" }.Where(name => spec.TryGetProperty(name, out var value) &&
                                                                           value.ValueKind != JsonValueKind.String))
        {
            yield return new SpecError($"spec.{name}", "Must be the name of an Asset Type or Asset.");
        }

        foreach (var error in DataPointDeclarations.Validate(spec))
        {
            yield return error;
        }

        var assetType = Text(spec, "assetType") is { } typeName
            ? @namespace.Find(AssetTypeKind.KindName, typeName)
            : null;
        foreach (var error in _sources.ValidateAsset(spec, assetType, @namespace))
        {
            yield return error;
        }

        foreach (var error in AlarmDeclarations.Validate(
                     spec,
                     [
                         .. assetType is { } type ? DataPointDeclarations.Declarations(type) : [],
                         .. DataPointDeclarations.Declarations(spec)
                     ],
                     assetType is { } declaring ? AlarmDeclarations.Names(declaring).ToHashSet() : []))
        {
            yield return error;
        }

        foreach (var error in Clashes(spec, @namespace))
        {
            yield return error;
        }

        if (InCycle(spec, @namespace))
        {
            yield return new SpecError("spec.parent", "The parents of this Asset loop back on themselves.");
        }
    }

    public IEnumerable<SpecReference> References(JsonElement spec)
    {
        if (Text(spec, "assetType") is { } assetType)
        {
            yield return new SpecReference(AssetTypeKind.KindName, assetType, "spec.assetType");
        }

        if (Text(spec, "parent") is { } parent)
        {
            yield return new SpecReference(KindName, parent, "spec.parent");
        }

        foreach (var reference in AssetSources.References(spec))
        {
            yield return reference;
        }
    }

    private static IEnumerable<SpecError> Clashes(JsonElement spec, NamespaceSnapshot @namespace)
    {
        if (Text(spec, "assetType") is not { } typeName ||
            @namespace.Find(AssetTypeKind.KindName, typeName) is not { } type)
        {
            yield break;
        }

        var declared = DataPointDeclarations.Names(type).ToHashSet();
        var index = 0;
        foreach (var name in DataPointDeclarations.Names(spec))
        {
            if (declared.Contains(name))
            {
                yield return new SpecError(
                    $"spec.dataPoints[{index}].name",
                    $"Asset Type '{typeName}' already declares Data Point '{name}', which an instance cannot redefine.");
            }

            index++;
        }
    }

    private static bool InCycle(JsonElement spec, NamespaceSnapshot @namespace)
    {
        var visited = new HashSet<string>();
        var next = Text(spec, "parent");
        while (next is not null)
        {
            if (!visited.Add(next))
            {
                return true;
            }

            next = @namespace.Find(KindName, next) is { } parent ? Text(parent, "parent") : null;
        }

        return false;
    }

    private static string? Text(JsonElement spec, string name) =>
        spec.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
