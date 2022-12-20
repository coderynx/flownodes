using Flownodes.Shared.Models;

namespace Flownodes.Shared.Interfaces;

[GenerateSerializer]
public sealed record Alert([property: Id(0)] Guid Id, [property: Id(1)] string TargetResourceId,
    [property: Id(2)] AlertSeverity Severity, [property: Id(3)] DateTime CreatedAt,
    [property: Id(4)] string Description);