using System.Text.Json;
using Registry.Domain.Namespaces;

namespace Registry.Domain.Resources;

/// <summary>
/// A recorded Spec of a Resource, with who applied it and when. A deletion is recorded as a tombstone with no Spec.
/// </summary>
public sealed class Revision
{
    private Revision()
    {
    }

    /// <summary>
    /// Gets the database identity, which orders a Resource's Revisions from oldest to newest.
    /// </summary>
    public long Id { get; private set; }
    /// <summary>
    /// Gets the Namespace of the Resource; a Namespace is its own Namespace.
    /// </summary>
    public required NamespaceName Namespace { get; init; }
    /// <summary>
    /// Gets the Kind of the Resource.
    /// </summary>
    public required string Kind { get; init; }
    /// <summary>
    /// Gets the name of the Resource.
    /// </summary>
    public required ResourceName Name { get; init; }
    /// <summary>
    /// Gets the incarnation of the Resource the Revision belongs to.
    /// </summary>
    public required Guid Uid { get; init; }
    /// <summary>
    /// Gets the Generation the Spec started, or the last one reached for a tombstone.
    /// </summary>
    public required long Generation { get; init; }
    /// <summary>
    /// Gets the Spec as applied, or <see langword="null"/> for a tombstone.
    /// </summary>
    public JsonElement? Spec { get; private set; }
    /// <summary>
    /// Gets a value indicating whether the Revision records the deletion of the Resource.
    /// </summary>
    public bool Deleted { get; private set; }
    /// <summary>
    /// Gets who made the change.
    /// </summary>
    public required RevisionAuthor Author { get; init; }
    /// <summary>
    /// Gets when the change was committed.
    /// </summary>
    public required DateTimeOffset AppliedAt { get; init; }

    /// <summary>
    /// Records the Spec a Resource now has.
    /// </summary>
    /// <param name="resource">The Resource, as created or changed.</param>
    /// <param name="author">Who applied it.</param>
    /// <param name="now">When.</param>
    /// <returns>The Revision.</returns>
    public static Revision Applied(Resource resource, RevisionAuthor author, DateTimeOffset now) =>
        new()
        {
            Namespace = resource.Namespace,
            Kind = resource.Kind,
            Name = resource.Name,
            Uid = resource.Uid,
            Generation = resource.Generation,
            Spec = resource.Spec.Clone(),
            Author = author,
            AppliedAt = now
        };

    /// <summary>
    /// Records the creation of a Namespace, whose Spec is empty.
    /// </summary>
    /// <param name="namespace">The Namespace created.</param>
    /// <param name="author">Who applied it.</param>
    /// <param name="now">When.</param>
    /// <returns>The Revision.</returns>
    public static Revision Applied(Namespace @namespace, RevisionAuthor author, DateTimeOffset now) =>
        new()
        {
            Namespace = @namespace.Name,
            Kind = Namespaces.Namespace.Kind,
            Name = ResourceName.Create(@namespace.Name.Value),
            Uid = @namespace.Uid,
            Generation = @namespace.Generation,
            Spec = Namespaces.Namespace.Spec,
            Author = author,
            AppliedAt = now
        };

    /// <summary>
    /// Records the deletion of a Resource.
    /// </summary>
    /// <param name="resource">The Resource deleted.</param>
    /// <param name="author">Who deleted it.</param>
    /// <param name="now">When.</param>
    /// <returns>The tombstone.</returns>
    public static Revision Tombstone(Resource resource, RevisionAuthor author, DateTimeOffset now) =>
        new()
        {
            Namespace = resource.Namespace,
            Kind = resource.Kind,
            Name = resource.Name,
            Uid = resource.Uid,
            Generation = resource.Generation,
            Deleted = true,
            Author = author,
            AppliedAt = now
        };

    /// <summary>
    /// Records the deletion of a Namespace.
    /// </summary>
    /// <param name="namespace">The Namespace deleted.</param>
    /// <param name="author">Who deleted it.</param>
    /// <param name="now">When.</param>
    /// <returns>The tombstone.</returns>
    public static Revision Tombstone(Namespace @namespace, RevisionAuthor author, DateTimeOffset now) =>
        new()
        {
            Namespace = @namespace.Name,
            Kind = Namespaces.Namespace.Kind,
            Name = ResourceName.Create(@namespace.Name.Value),
            Uid = @namespace.Uid,
            Generation = @namespace.Generation,
            Deleted = true,
            Author = author,
            AppliedAt = now
        };
}
