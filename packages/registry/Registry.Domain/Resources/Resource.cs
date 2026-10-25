using System.Text.Json;
using Registry.Domain.Namespaces;

namespace Registry.Domain.Resources;

/// <summary>
/// A namespaced Resource as the Registry stores it: its identity, its current Spec and Generation, and the Status the
/// Engine last reported for it.
/// </summary>
public sealed class Resource
{
    private Resource()
    {
    }

    /// <summary>
    /// The <c>apiVersion</c> every Resource is written with in a Manifest.
    /// </summary>
    public const string ApiVersion = "flownodes.io/v1alpha1";

    /// <summary>
    /// Gets the Namespace the Resource lives in.
    /// </summary>
    public required NamespaceName Namespace { get; init; }
    /// <summary>
    /// Gets the Kind of the Resource, such as <c>Connection</c>.
    /// </summary>
    public required string Kind { get; init; }
    /// <summary>
    /// Gets the name of the Resource, unique per Kind within its Namespace.
    /// </summary>
    public required ResourceName Name { get; init; }
    /// <summary>
    /// Gets the identifier of this incarnation of the Resource; a re-created Resource has a new one.
    /// </summary>
    public required Guid Uid { get; init; }
    /// <summary>
    /// Gets the current Spec, the latest Revision.
    /// </summary>
    public JsonElement Spec { get; private set; }
    /// <summary>
    /// Gets the Generation, which increases every time the Spec changes.
    /// </summary>
    public long Generation { get; private set; }
    /// <summary>
    /// Gets the Namespace's sequence number of the last message published about the Resource.
    /// </summary>
    public long Seq { get; private set; }
    /// <summary>
    /// Gets the Status the Engine last reported, or <see langword="null"/> before it has reported one.
    /// </summary>
    public JsonElement? Status { get; private set; }
    /// <summary>
    /// Gets the Generation the Engine last picked up, or <see langword="null"/> before it has reported a Status.
    /// </summary>
    public long? ObservedGeneration { get; private set; }
    /// <summary>
    /// Gets the Engine's sequence number of the last Status kept.
    /// </summary>
    public long StatusSeq { get; private set; }

    /// <summary>
    /// Creates a Resource at Generation 1 with a new <see cref="Uid"/>.
    /// </summary>
    /// <param name="namespace">The Namespace the Resource lives in.</param>
    /// <param name="kind">The Kind of the Resource.</param>
    /// <param name="name">The name of the Resource.</param>
    /// <param name="spec">The Spec as applied.</param>
    /// <param name="seq">The Namespace's sequence number the creation message carries.</param>
    /// <returns>The new Resource.</returns>
    public static Resource Create(
        NamespaceName @namespace,
        string kind,
        ResourceName name,
        JsonElement spec,
        long seq) =>
        new()
        {
            Namespace = @namespace,
            Kind = kind,
            Name = name,
            Uid = Guid.NewGuid(),
            Spec = spec.Clone(),
            Generation = 1,
            Seq = seq
        };

    /// <summary>
    /// Tells whether a Spec means the same as the current one, so applying it would change nothing.
    /// </summary>
    /// <param name="spec">The Spec being applied.</param>
    /// <returns><see langword="true"/> when applying <paramref name="spec"/> is a no-op.</returns>
    public bool Matches(JsonElement spec) => SpecDiff.AreEquivalent(Spec, spec);

    /// <summary>
    /// Tells which Generation applying a Spec would leave the Resource at, without applying it.
    /// </summary>
    /// <param name="spec">The Spec being applied.</param>
    /// <returns>The current Generation when the Spec means the same, otherwise the next one.</returns>
    public long GenerationAfter(JsonElement spec) => Matches(spec) ? Generation : Generation + 1;

    /// <summary>
    /// Replaces the whole Spec, which starts a new Generation.
    /// </summary>
    /// <param name="spec">The new Spec.</param>
    /// <param name="seq">The Namespace's sequence number the change message carries.</param>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="spec"/> means the same as the current Spec.
    /// </exception>
    public void Apply(JsonElement spec, long seq)
    {
        if (Matches(spec))
        {
            throw new InvalidOperationException("Applying an unchanged Spec starts no Generation.");
        }

        Spec = spec.Clone();
        Generation++;
        Seq = seq;
    }

    /// <summary>
    /// Advances the sequence number to the one the deletion message carries.
    /// </summary>
    /// <param name="seq">The Namespace's sequence number the deletion message carries.</param>
    public void Delete(long seq)
    {
        Seq = seq;
    }

    /// <summary>
    /// Records a Status the Engine reported, unless it belongs to another incarnation or is not newer than the last.
    /// </summary>
    /// <param name="uid">The incarnation the Status is about.</param>
    /// <param name="statusSeq">The Engine's sequence number for the Status.</param>
    /// <param name="observedGeneration">The Generation the Engine has picked up.</param>
    /// <param name="status">The Status, without its <c>observedGeneration</c>.</param>
    /// <returns><see langword="true"/> when the Status was kept.</returns>
    public bool ReportStatus(Guid uid, long statusSeq, long observedGeneration, JsonElement status)
    {
        if (uid != Uid || statusSeq <= StatusSeq)
        {
            return false;
        }

        Status = status.Clone();
        ObservedGeneration = observedGeneration;
        StatusSeq = statusSeq;

        return true;
    }
}
