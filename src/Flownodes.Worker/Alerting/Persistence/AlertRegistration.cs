namespace Flownodes.Worker.Alerting.Persistence;

[GenerateSerializer]
internal sealed record AlertRegistration([property: Id(0)] string AlertName, [property: Id(1)] string TargetObjectName);