namespace Core.Application.Authorization;

/// <summary>
/// The Namespaces a caller may see.
/// </summary>
/// <param name="All">Whether the caller sees every Namespace, as an Administrator does when listing them.</param>
/// <param name="Names">The names of the Namespaces where the caller holds a role.</param>
public sealed record NamespaceScope(bool All, IReadOnlySet<string> Names)
{
    /// <summary>
    /// Tells whether a Namespace is within the scope.
    /// </summary>
    /// <param name="name">The name of the Namespace.</param>
    /// <returns><see langword="true"/> when the caller sees the Namespace.</returns>
    public bool Includes(string name) => All || Names.Contains(name);
}
