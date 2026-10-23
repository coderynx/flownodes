using Coderynx.Functional.Results.Errors;

namespace Registry.Application.Namespaces;

internal static class Errors
{
    public static Error InvalidName(string name) =>
        Error.InvalidInput(
            "namespace.invalid_name",
            $"'{name}' is not a valid Namespace name. Use a DNS label: lowercase letters, digits and hyphens, " +
            "at most 63 characters, starting and ending with a letter or a digit.");

    public static Error NotFound(string name) =>
        Error.NotFound("namespace.not_found", $"Namespace '{name}' does not exist.");

    public static Error DefaultCannotBeDeleted() =>
        Error.Conflict("namespace.default_cannot_be_deleted", "The default Namespace cannot be deleted.");

    public static Error HoldsResources(string name) =>
        Error.Conflict("namespace.holds_resources", $"Namespace '{name}' still holds Resources. Delete them first.");

    public static Error UnknownSortKey(string key) =>
        Error.InvalidInput("namespace.unknown_sort_key", $"Cannot sort Namespaces by '{key}'. Use: name.");
}
