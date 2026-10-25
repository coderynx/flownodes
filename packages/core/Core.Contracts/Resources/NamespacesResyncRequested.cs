namespace Core.Contracts.Resources;

/// <summary>
/// Sent by the Engine to have the Registry publish Namespaces again (ADR-0014): by every Silo when it starts, and by
/// a Namespace's grain when it activates, finds a gap in the sequence numbers or has not resynced for a while.
/// </summary>
/// <param name="Namespace">The name of the one Namespace to publish again, or <see langword="null"/> for all.</param>
public sealed record NamespacesResyncRequested(string? Namespace = null);
