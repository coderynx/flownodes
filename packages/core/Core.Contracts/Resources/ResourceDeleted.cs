namespace Core.Contracts.Resources;

/// <summary>
/// Published by the Registry when a Resource is deleted (ADR-0014). It is the last message about that incarnation
/// of the Resource.
/// </summary>
/// <param name="Namespace">The name of the Namespace the Resource lived in; a Namespace is its own Namespace.</param>
/// <param name="Kind">The Kind of the Resource, such as <c>Namespace</c>.</param>
/// <param name="Name">The Resource's name within its Namespace.</param>
/// <param name="Uid">Identifies the incarnation of the Resource that was deleted.</param>
/// <param name="Generation">The last Generation the Resource reached.</param>
/// <param name="Seq">
/// The Namespace's gap-free sequence number for this message. A receiver drops a message whose number is not newer
/// than the last it applied.
/// </param>
public sealed record ResourceDeleted(string Namespace, string Kind, string Name, Guid Uid, long Generation, long Seq);
