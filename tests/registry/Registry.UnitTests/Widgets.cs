using System.Text.Json;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;

namespace Registry.UnitTests;

/// <summary>
/// A namespaced Kind for tests only: a Widget has an optional integer <c>size</c> and may reference a parent Widget.
/// </summary>
internal sealed class WidgetKind : IResourceKind
{
    public const string Kind = "Widget";
    public static KindCatalog Catalog { get; } = new([new WidgetKind()]);
    public string Name => Kind;
    public string Plural => "widgets";

    public IEnumerable<SpecError> Validate(JsonElement spec)
    {
        if (spec.TryGetProperty("size", out var size) && size.ValueKind != JsonValueKind.Number)
        {
            yield return new SpecError("spec.size", "Must be a number.");
        }
    }

    public IEnumerable<SpecReference> References(JsonElement spec)
    {
        if (spec.TryGetProperty("parent", out var parent) && parent.GetString() is { } name)
        {
            yield return new SpecReference(Kind, name, "spec.parent");
        }
    }
}

internal static class Widgets
{
    public static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    public static JsonElement Document(
        string name,
        string spec = "{}",
        string? @namespace = null,
        long? generation = null)
    {
        var metadata = new Dictionary<string, object> { ["name"] = name };
        if (@namespace is not null)
        {
            metadata["namespace"] = @namespace;
        }

        if (generation is not null)
        {
            metadata["generation"] = generation;
        }

        return JsonSerializer.SerializeToElement(
            new Dictionary<string, object>
            {
                ["apiVersion"] = "flownodes.io/v1alpha1",
                ["kind"] = WidgetKind.Kind,
                ["metadata"] = metadata,
                ["spec"] = Json(spec)
            });
    }

    public static Resource Stored(string @namespace, string name, string spec = "{}", long seq = 2) =>
        Resource.Create(NamespaceName.Create(@namespace), WidgetKind.Kind, ResourceName.Create(name), Json(spec), seq);
}
