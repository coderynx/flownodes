using System.Text.Json;
using Registry.Domain.Resources;

namespace Registry.IntegrationTests;

/// <summary>
/// A namespaced Kind the test host registers: a Widget has an optional integer <c>size</c>, no larger than its
/// parent's, and may reference a parent Widget. That is enough to drive apply, references and deletes until the first
/// real namespaced Kind exists.
/// </summary>
public sealed class WidgetKind : IResourceKind
{
    public string Name => "Widget";
    public string Plural => "widgets";

    public IEnumerable<SpecError> Validate(JsonElement spec, NamespaceSnapshot @namespace)
    {
        if (spec.TryGetProperty("size", out var size) && size.ValueKind != JsonValueKind.Number)
        {
            yield return new SpecError("spec.size", "Must be a number.");

            yield break;
        }

        var parent = References(spec).FirstOrDefault();
        var parentSpec = parent is null ? null : @namespace.Find(parent.Kind, parent.Name);
        var exceeds = parentSpec is { } found && Size(found) is { } limit && Size(spec) > limit;
        if (exceeds)
        {
            yield return new SpecError("spec.size", "Must not exceed the parent's size.");
        }
    }

    public IEnumerable<SpecReference> References(JsonElement spec)
    {
        if (spec.TryGetProperty("parent", out var parent) && parent.GetString() is { } name)
        {
            yield return new SpecReference(Name, name, "spec.parent");
        }

        if (spec.TryGetProperty("secret", out var secret) && secret.GetString() is { } secretName)
        {
            yield return new SpecReference(SecretKind.KindName, secretName, "spec.secret");
        }
    }

    private static decimal? Size(JsonElement spec) =>
        spec.TryGetProperty("size", out var size) && size.ValueKind == JsonValueKind.Number ? size.GetDecimal() : null;
}
