using System.Text.Json;

namespace Core.Contracts.Resources;

/// <summary>
/// Sent by the Engine when the Status of a Resource changes (ADR-0014). The Registry stores it beside the Resource,
/// so reading a Resource never asks the Engine.
/// </summary>
/// <param name="Namespace">The name of the Namespace the Resource lives in.</param>
/// <param name="Kind">The Kind of the Resource, such as <c>Connection</c>.</param>
/// <param name="Name">The Resource's name within its Namespace.</param>
/// <param name="Uid">
/// The incarnation of the Resource the Status is about; a Status about another incarnation is dropped.
/// </param>
/// <param name="ObservedGeneration">The Generation of the Resource the Engine has picked up.</param>
/// <param name="StatusSeq">
/// The Engine's sequence number for this Resource's Status. The Registry drops a Status whose number is not newer
/// than the last it stored.
/// </param>
/// <param name="Status">
/// The Status: its <c>conditions</c> and the Kind's own fields, without <c>observedGeneration</c>.
/// </param>
public sealed record StatusReported(
    string Namespace,
    string Kind,
    string Name,
    Guid Uid,
    long ObservedGeneration,
    long StatusSeq,
    JsonElement Status);
