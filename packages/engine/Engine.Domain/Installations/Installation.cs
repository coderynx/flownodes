namespace Engine.Domain.Installations;

/// <summary>
/// What identifies the Installation: the permanent identifier it generated when it first started and the Flownodes
/// version this Silo runs.
/// </summary>
/// <param name="Id">The identifier, the same on every Silo while the Installation keeps its stored state.</param>
/// <param name="Version">The Flownodes version of this Silo.</param>
public sealed record Installation(Guid Id, string Version);
