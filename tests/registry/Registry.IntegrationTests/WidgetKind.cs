using System.Text.Json;
using Registry.Domain.Resources;

namespace Registry.IntegrationTests;

/// <summary>
/// A namespaced Kind the test host registers: a Widget has an optional integer <c>size</c> and may reference a parent
/// Widget, which is enough to drive apply, references and deletes until the first real namespaced Kind exists.
/// </summary>
public sealed class WidgetKind : IResourceKind
{
    public string Name => "Widget";
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
            yield return new SpecReference(Name, name, "spec.parent");
        }
    }
}
