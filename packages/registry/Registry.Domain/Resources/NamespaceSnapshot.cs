using System.Text.Json;
using Registry.Domain.Namespaces;

namespace Registry.Domain.Resources;

/// <summary>
/// The namespaced Resources of one Namespace as an apply would leave them, which a Kind's rules can look across, such
/// as an Asset checking that it still fits its changed Asset Type (ADR-0007).
/// </summary>
public sealed class NamespaceSnapshot
{
    private readonly List<SnapshotResource> _resources;

    /// <summary>
    /// Initializes a new instance of the <see cref="NamespaceSnapshot"/> class.
    /// </summary>
    /// <param name="name">The Namespace.</param>
    /// <param name="resources">Every namespaced Resource in it.</param>
    public NamespaceSnapshot(NamespaceName name, IEnumerable<SnapshotResource> resources)
    {
        Name = name;
        _resources = [.. resources];
    }

    /// <summary>
    /// Gets the Namespace the snapshot is of.
    /// </summary>
    public NamespaceName Name { get; }

    /// <summary>
    /// Finds the Spec of one Resource.
    /// </summary>
    /// <param name="kind">The Kind of the Resource.</param>
    /// <param name="name">The name of the Resource.</param>
    /// <returns>The Spec, or <see langword="null"/> when the Namespace holds no such Resource.</returns>
    public JsonElement? Find(string kind, string name) =>
        _resources.Find(resource => resource.Kind == kind && resource.Name.Value == name)?.Spec;

    /// <summary>
    /// Lists the Resources of one Kind.
    /// </summary>
    /// <param name="kind">The Kind.</param>
    /// <returns>The Resources of that Kind.</returns>
    public IReadOnlyList<SnapshotResource> OfKind(string kind) =>
        [.. _resources.Where(resource => resource.Kind == kind)];
}

/// <summary>
/// One Resource in a <see cref="NamespaceSnapshot"/>.
/// </summary>
/// <param name="Kind">The Kind of the Resource.</param>
/// <param name="Name">The name of the Resource.</param>
/// <param name="Spec">The Spec the Resource has after the apply.</param>
public sealed record SnapshotResource(string Kind, ResourceName Name, JsonElement Spec);
