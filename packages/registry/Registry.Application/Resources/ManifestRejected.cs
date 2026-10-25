using Coderynx.Functional.Results.Errors;

namespace Registry.Application.Resources;

/// <summary>
/// The error returned when a Manifest has problems: nothing in it was applied or deleted (ADR-0007).
/// </summary>
/// <param name="Problems">Every problem found, ordered by document.</param>
public sealed record ManifestRejected(IReadOnlyList<ManifestProblem> Problems) : Error(
    ErrorKind.InvalidInput,
    "manifest.invalid",
    $"The Manifest has {Problems.Count} problem(s), so nothing in it was changed.");

/// <summary>
/// One problem with one Resource in a Manifest.
/// </summary>
/// <param name="Document">
/// The position of the Resource in the Manifest, counting from 0, or <see langword="null"/> for a Resource the Manifest
/// does not list but whose meaning it changes.
/// </param>
/// <param name="Kind">The Kind of the Resource, or <see langword="null"/> when it could not be read.</param>
/// <param name="Name">The name of the Resource, or <see langword="null"/> when it could not be read.</param>
/// <param name="Path">Where the problem is, such as <c>metadata.name</c> or <c>spec.connection</c>.</param>
/// <param name="Message">What is wrong, for the Engineer who applied it.</param>
public sealed record ManifestProblem(int? Document, string? Kind, string? Name, string Path, string Message);
