using Coderynx.Functional.Results.Errors;

namespace Registry.Application.Resources;

internal static class Errors
{
    public static Error EmptyManifest() =>
        Error.InvalidInput("manifest.empty", "The Manifest holds no Resources.");

    public static Error NamespaceNotFound(string name) =>
        Error.NotFound("namespace.not_found", $"Namespace '{name}' does not exist.");

    public static Error KindNotFound(string plural) =>
        Error.NotFound("kind.not_found", $"There is no Kind called '{plural}'.");

    public static Error NotFound(string kind, string name) =>
        Error.NotFound("resource.not_found", $"{kind} '{name}' does not exist.");

    public static Error GenerationConflict(string kind, string name, long expected, long? actual) =>
        Error.Conflict(
            "resource.generation_conflict",
            actual is null
                ? $"{kind} '{name}' was applied at Generation {expected}, but it no longer exists."
                : $"{kind} '{name}' was applied at Generation {expected}, but it is now at Generation {actual}. " +
                  "Read it again and reapply.");

    public static Error Referenced(string kind, string name, IEnumerable<string> referrers) =>
        Error.Conflict(
            "resource.referenced",
            $"{kind} '{name}' is still referenced by {string.Join(", ", referrers)}. Delete or change those first.");

    public static Error DefaultNamespace() =>
        Error.Conflict("namespace.default_cannot_be_deleted", "The default Namespace cannot be deleted.");

    public static Error NamespaceHoldsResources(string name) =>
        Error.Conflict("namespace.holds_resources", $"Namespace '{name}' still holds Resources. Delete them first.");

    public static Error UnknownSortKey(string key) =>
        Error.InvalidInput("resource.unknown_sort_key", $"Cannot sort Resources by '{key}'. Use: name.");
}
