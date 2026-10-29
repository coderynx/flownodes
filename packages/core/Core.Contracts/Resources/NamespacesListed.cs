namespace Core.Contracts.Resources;

/// <summary>
/// The Registry's answer to <see cref="NamespacesListRequested"/>: the Engine wakes the grain of each Namespace named,
/// so a Namespace it has no state for asks for a resync (ADR-0014).
/// </summary>
/// <param name="Names">The name of every current Namespace; empty when there are none.</param>
public sealed record NamespacesListed(IReadOnlyList<string> Names);
