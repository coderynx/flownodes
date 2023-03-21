using Flownodes.Sdk.Alerting;

namespace Flownodes.Worker.Alerting.Persistence;

[GenerateSerializer]
internal sealed record AlertPersistence
{
    [Id(0)] public string? TargetObjectName { get; set; }

    [Id(1)] public DateTime FiredAt { get; set; }

    [Id(2)] public AlertSeverity Severity { get; set; }

    [Id(3)] public string? Description { get; set; }
    [Id(4)] public ISet<string> DriverIds { get; set; } = new HashSet<string>();
}