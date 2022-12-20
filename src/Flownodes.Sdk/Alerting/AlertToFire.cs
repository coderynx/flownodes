namespace Flownodes.Sdk.Alerting;

public enum AlertToFireSeverity
{
    Verbose,
    Informational,
    Warning,
    Error,
    Critical
}

public record AlertToFire
{
    public Guid Id { get; init; }
    public DateTime FiredAt { get; init; }
    public AlertToFireSeverity Severity { get; init; }
    public string TargetResourceId { get; init; }
    public string Description { get; init; }
}