using System.Text.Json;
using System.Text.RegularExpressions;

namespace Registry.Domain.Resources;

/// <summary>
/// The Secret Kind: named credentials other Resources reference. A Secret is applied with its values in
/// <c>spec.data</c>, but the Registry keeps only a fingerprint of each in the Spec; the values themselves are held
/// encrypted apart from the Resource and its Revisions and are never read back.
/// </summary>
public sealed partial class SecretKind : IResourceKind
{
    /// <summary>
    /// The name of the Kind in a Manifest.
    /// </summary>
    public const string KindName = "Secret";

    /// <summary>
    /// The text that stands in for a value wherever a change to a Secret is reported.
    /// </summary>
    public const string Hidden = "(hidden)";

    private const int MaxValueLength = 64 * 1024;
    /// <inheritdoc />
    public string Name => KindName;
    /// <inheritdoc />
    public string Plural => "secrets";

    /// <summary>
    /// Replaces each value of an applied Secret with its fingerprint.
    /// </summary>
    /// <param name="spec">A Spec that passed <see cref="Validate"/>.</param>
    /// <param name="fingerprint">Maps a value to a fingerprint that cannot be reversed to it.</param>
    /// <returns>The Spec as the Registry stores it.</returns>
    public static JsonElement Seal(JsonElement spec, Func<string, string> fingerprint) =>
        JsonSerializer.SerializeToElement(
            new Dictionary<string, object>
            {
                ["data"] = Values(spec).ToDictionary(entry => entry.Key, entry => fingerprint(entry.Value))
            });

    /// <summary>
    /// Reads the values of an applied Secret.
    /// </summary>
    /// <param name="spec">A Spec that passed <see cref="Validate"/>.</param>
    /// <returns>Each key with its value.</returns>
    public static IReadOnlyDictionary<string, string> Values(JsonElement spec) =>
        spec.GetProperty("data").EnumerateObject().ToDictionary(field => field.Name, field => field.Value.GetString()!);

    /// <summary>
    /// Reduces a stored Secret Spec to what may be shown: the names of its keys.
    /// </summary>
    /// <param name="spec">The Spec as stored.</param>
    /// <returns>A Spec with a <c>keys</c> list and nothing else.</returns>
    public static JsonElement Redact(JsonElement spec) =>
        JsonSerializer.SerializeToElement(
            new Dictionary<string, object>
            {
                ["keys"] = spec.GetProperty("data").EnumerateObject().Select(field => field.Name).Order().ToList()
            });

    /// <summary>
    /// Replaces the values in a list of changes with <see cref="Hidden"/>, so a diff shows that a key changed and
    /// nothing of what it held.
    /// </summary>
    /// <param name="changes">The changes between two stored Secret Specs.</param>
    /// <returns>The same changes without their values.</returns>
    public static IReadOnlyList<SpecChange> Hide(IReadOnlyList<SpecChange> changes)
    {
        var hidden = JsonSerializer.SerializeToElement(Hidden);

        return
        [
            .. changes.Select(change => change with
            {
                From = change.From is null ? null : hidden,
                To = change.To is null ? null : hidden
            })
        ];
    }

    /// <inheritdoc />
    public IEnumerable<SpecError> Validate(JsonElement spec, NamespaceSnapshot @namespace)
    {
        foreach (var field in spec.EnumerateObject().Where(field => field.Name != "data"))
        {
            yield return new SpecError($"spec.{field.Name}", "Unknown field.");
        }

        if (!spec.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            yield return new SpecError("spec.data", "Required, as an object of key names and values.");

            yield break;
        }

        if (!data.EnumerateObject().Any())
        {
            yield return new SpecError("spec.data", "Must hold at least one key.");
        }

        foreach (var field in data.EnumerateObject())
        {
            if (!KeyName().IsMatch(field.Name))
            {
                yield return new SpecError(
                    $"spec.data.{field.Name}",
                    "A key name has letters, digits, hyphens, underscores and dots, at most 253 characters.");
            }

            if (field.Value.ValueKind != JsonValueKind.String || field.Value.GetString()!.Length == 0)
            {
                yield return new SpecError($"spec.data.{field.Name}", "Must be a text that is not empty.");
            }
            else if (field.Value.GetString()!.Length > MaxValueLength)
            {
                yield return new SpecError($"spec.data.{field.Name}", $"Must be at most {MaxValueLength} characters.");
            }
        }
    }

    /// <inheritdoc />
    public IEnumerable<SpecReference> References(JsonElement spec) => [];

    [GeneratedRegex("^[A-Za-z0-9._-]{1,253}$")]
    private static partial Regex KeyName();
}
