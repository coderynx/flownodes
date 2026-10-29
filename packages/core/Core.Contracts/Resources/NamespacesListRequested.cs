namespace Core.Contracts.Resources;

/// <summary>
/// Sent by every Silo when it starts and then at an interval, to have the Registry answer with the name of every
/// Namespace (ADR-0014).
/// </summary>
public sealed record NamespacesListRequested;
