using System.Text.Json;

namespace Core.Contracts.Resources;

/// <summary>
/// Published by the Registry when a Resource is created or its Spec changes (ADR-0014). It carries the whole Spec, so
/// a receiver never reads it back from the Registry.
/// </summary>
/// <param name="Namespace">The name of the Namespace the Resource lives in; a Namespace is its own Namespace.</param>
/// <param name="Kind">The Kind of the Resource, such as <c>Namespace</c>.</param>
/// <param name="Name">The Resource's name within its Namespace.</param>
/// <param name="Uid">Identifies this incarnation of the Resource; a re-created Resource has a new one.</param>
/// <param name="Generation">The Generation of the Resource this Spec belongs to.</param>
/// <param name="Seq">
/// The Namespace's gap-free sequence number for this message. A receiver drops a message whose number is not newer
/// than the last it applied.
/// </param>
/// <param name="Spec">The Resource's whole Spec.</param>
public sealed record ResourceApplied(
    string Namespace,
    string Kind,
    string Name,
    Guid Uid,
    long Generation,
    long Seq,
    JsonElement Spec);
