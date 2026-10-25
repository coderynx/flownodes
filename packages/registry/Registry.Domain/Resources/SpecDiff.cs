using System.Text.Json;

namespace Registry.Domain.Resources;

/// <summary>
/// Compares two Specs by meaning rather than by text: the order of an object's fields and the way a number is written
/// do not count as a change, so a Spec read back from the database equals the one applied.
/// </summary>
public static class SpecDiff
{
    private const string Root = "spec";

    /// <summary>
    /// Lists what applying a Spec would change, one entry per changed value, in the order the fields appear.
    /// </summary>
    /// <param name="before">The current Spec, or <see langword="null"/> when the Resource does not exist yet.</param>
    /// <param name="after">The Spec being applied.</param>
    /// <returns>The changes; empty when both Specs mean the same.</returns>
    public static IReadOnlyList<SpecChange> Between(JsonElement? before, JsonElement after)
    {
        var changes = new List<SpecChange>();
        if (before is null)
        {
            if (after.ValueKind == JsonValueKind.Object)
            {
                changes.AddRange(
                    after.EnumerateObject()
                        .Select(field => new SpecChange(
                            $"{Root}.{field.Name}",
                            SpecChangeKind.Added,
                            null,
                            field.Value.Clone())));
            }

            return changes;
        }

        Compare(Root, before.Value, after, changes);

        return changes;
    }

    /// <summary>
    /// Tells whether two Specs mean the same.
    /// </summary>
    /// <param name="first">One Spec.</param>
    /// <param name="second">The other Spec.</param>
    /// <returns><see langword="true"/> when applying one over the other would change nothing.</returns>
    public static bool AreEquivalent(JsonElement first, JsonElement second) => Between(first, second).Count == 0;

    private static void Compare(string path, JsonElement before, JsonElement after, List<SpecChange> changes)
    {
        if (before.ValueKind == JsonValueKind.Object && after.ValueKind == JsonValueKind.Object)
        {
            CompareObjects(path, before, after, changes);
        }
        else if (before.ValueKind == JsonValueKind.Array && after.ValueKind == JsonValueKind.Array)
        {
            CompareArrays(path, before, after, changes);
        }
        else if (!ScalarsEqual(before, after))
        {
            changes.Add(new SpecChange(path, SpecChangeKind.Changed, before.Clone(), after.Clone()));
        }
    }

    private static void CompareObjects(string path, JsonElement before, JsonElement after, List<SpecChange> changes)
    {
        var afterFields = after.EnumerateObject().ToDictionary(field => field.Name, field => field.Value);

        foreach (var field in before.EnumerateObject())
        {
            var fieldPath = $"{path}.{field.Name}";
            if (afterFields.TryGetValue(field.Name, out var value))
            {
                Compare(fieldPath, field.Value, value, changes);
            }
            else
            {
                changes.Add(new SpecChange(fieldPath, SpecChangeKind.Removed, field.Value.Clone(), null));
            }
        }

        var beforeNames = before.EnumerateObject().Select(field => field.Name).ToHashSet();
        changes.AddRange(
            after.EnumerateObject()
                .Where(field => !beforeNames.Contains(field.Name))
                .Select(field => new SpecChange(
                    $"{path}.{field.Name}",
                    SpecChangeKind.Added,
                    null,
                    field.Value.Clone())));
    }

    private static void CompareArrays(string path, JsonElement before, JsonElement after, List<SpecChange> changes)
    {
        var beforeItems = before.EnumerateArray().ToList();
        var afterItems = after.EnumerateArray().ToList();

        for (var index = 0; index < Math.Max(beforeItems.Count, afterItems.Count); index++)
        {
            var itemPath = $"{path}[{index}]";
            if (index >= afterItems.Count)
            {
                changes.Add(new SpecChange(itemPath, SpecChangeKind.Removed, beforeItems[index].Clone(), null));
            }
            else if (index >= beforeItems.Count)
            {
                changes.Add(new SpecChange(itemPath, SpecChangeKind.Added, null, afterItems[index].Clone()));
            }
            else
            {
                Compare(itemPath, beforeItems[index], afterItems[index], changes);
            }
        }
    }

    private static bool ScalarsEqual(JsonElement before, JsonElement after)
    {
        if (before.ValueKind != after.ValueKind)
        {
            return false;
        }

        return before.ValueKind switch
        {
            JsonValueKind.String => before.GetString() == after.GetString(),
            JsonValueKind.Number => before.TryGetDecimal(out var first) && after.TryGetDecimal(out var second)
                ? first == second
                : before.GetRawText() == after.GetRawText(),
            JsonValueKind.Object or JsonValueKind.Array => false,
            _ => true
        };
    }
}

/// <summary>
/// One value an apply adds, removes or changes in a Spec.
/// </summary>
/// <param name="Path">Where the value is, such as <c>spec.connection</c> or <c>spec.points[2]</c>.</param>
/// <param name="Kind">Whether the value is added, removed or changed.</param>
/// <param name="From">The value before the apply; <see langword="null"/> when it is added.</param>
/// <param name="To">The value after the apply; <see langword="null"/> when it is removed.</param>
public sealed record SpecChange(string Path, SpecChangeKind Kind, JsonElement? From, JsonElement? To);

/// <summary>
/// How an apply changes one value in a Spec.
/// </summary>
public enum SpecChangeKind
{
    /// <summary>
    /// The value is new.
    /// </summary>
    Added,

    /// <summary>
    /// The value is gone.
    /// </summary>
    Removed,

    /// <summary>
    /// The value is replaced by another.
    /// </summary>
    Changed
}
