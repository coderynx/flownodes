using Coderynx.Functional.Results.Errors;

namespace Registry.Application.Resources;

/// <summary>
/// The error returned when a delete would leave a reference dangling or a Namespace that cannot go: nothing was
/// deleted (ADR-0007).
/// </summary>
/// <param name="Problems">Every Resource that cannot be deleted, and why.</param>
public sealed record DeletionRefused(IReadOnlyList<ManifestProblem> Problems) : Error(
    ErrorKind.Conflict,
    "delete.refused",
    $"{Problems.Count} Resource(s) cannot be deleted, so nothing was deleted.");
