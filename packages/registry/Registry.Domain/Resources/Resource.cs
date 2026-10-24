using Registry.Domain.Namespaces;

namespace Registry.Domain.Resources;

/// <summary>
/// A namespaced Resource as the Registry stores it. Only its identity exists so far; a Namespace that holds any cannot
/// be deleted.
/// </summary>
public sealed class Resource
{
    private Resource()
    {
    }

    /// <summary>
    /// Gets the Namespace the Resource lives in.
    /// </summary>
    public required NamespaceName Namespace { get; init; }
    /// <summary>
    /// Gets the Kind of the Resource.
    /// </summary>
    public required string Kind { get; init; }
    /// <summary>
    /// Gets the name of the Resource within its Namespace.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Creates a Resource.
    /// </summary>
    /// <param name="namespace">The Namespace the Resource lives in.</param>
    /// <param name="kind">The Kind of the Resource.</param>
    /// <param name="name">The name of the Resource within its Namespace.</param>
    /// <returns>The new Resource.</returns>
    public static Resource Create(NamespaceName @namespace, string kind, string name) =>
        new()
        {
            Namespace = @namespace,
            Kind = kind,
            Name = name
        };
}
