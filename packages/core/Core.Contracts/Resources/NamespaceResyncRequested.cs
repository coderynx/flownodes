namespace Core.Contracts.Resources;

/// <summary>
/// Sent by a Namespace's grain to have the Registry publish that Namespace again (ADR-0014): when the grain
/// activates, finds a gap in the sequence numbers, has not resynced for a while, or is woken without live state.
/// </summary>
/// <param name="Namespace">The name of the Namespace to publish again.</param>
public sealed record NamespaceResyncRequested(string Namespace);
