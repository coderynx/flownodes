using System.Text.Json;

namespace Registry.Domain.Resources;

/// <summary>
/// The rules of one namespaced Kind: how its Spec is validated and which other Resources it references.
/// </summary>
/// <remarks>
/// The Registry applies, reads and deletes every namespaced Kind the same way and asks the Kind only what differs.
/// A Spec that passes <see cref="Validate"/> is stored as written; nothing is defaulted.
/// </remarks>
public interface IResourceKind
{
    /// <summary>
    /// Gets the Kind as written in a Manifest: PascalCase and singular, such as <c>Connection</c>.
    /// </summary>
    string Name { get; }
    /// <summary>
    /// Gets the Kind as written in a route and in <c>flowctl</c>: lowercase and plural, such as <c>connections</c>.
    /// </summary>
    string Plural { get; }

    /// <summary>
    /// Checks a Spec against the Kind's schema and rules, without looking at other Resources.
    /// </summary>
    /// <param name="spec">The Spec as applied.</param>
    /// <returns>Every problem found; empty when the Spec is valid.</returns>
    IEnumerable<SpecError> Validate(JsonElement spec);

    /// <summary>
    /// Lists the Resources a valid Spec references, each of which must exist in the same Namespace.
    /// </summary>
    /// <param name="spec">A Spec that passed <see cref="Validate"/>.</param>
    /// <returns>The references, with where each is written.</returns>
    IEnumerable<SpecReference> References(JsonElement spec);
}

/// <summary>
/// One problem with a Spec.
/// </summary>
/// <param name="Path">Where the problem is, such as <c>spec.connection</c>.</param>
/// <param name="Message">What is wrong, for the Engineer who applied it.</param>
public sealed record SpecError(string Path, string Message);

/// <summary>
/// One reference from a Spec to another Resource in the same Namespace.
/// </summary>
/// <param name="Kind">The Kind referenced, such as <c>Connection</c>.</param>
/// <param name="Name">The name of the Resource referenced.</param>
/// <param name="Path">Where the reference is written, such as <c>spec.connection</c>.</param>
public sealed record SpecReference(string Kind, string Name, string Path);
