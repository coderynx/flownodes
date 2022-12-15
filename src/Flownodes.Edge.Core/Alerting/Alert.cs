using Ardalis.GuardClauses;

namespace Flownodes.Edge.Core.Alerting;

[GenerateSerializer]
public record Alert
{
    public Alert(string frn, AlertKind kind, string message)
    {
        Guard.Against.NullOrWhiteSpace(frn, nameof(frn));
        Guard.Against.NullOrWhiteSpace(message, nameof(message));

        Frn = frn;
        Kind = kind;
        Message = message;
    }

    [Id(0)] public string Frn { get; }
    [Id(1)] public AlertKind Kind { get; }
    [Id(2)] public string Message { get; }
}