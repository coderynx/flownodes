using Ardalis.GuardClauses;

namespace Flownodes.Edge.Core.Alerting;

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

    public string Frn { get; }
    public AlertKind Kind { get; }
    public string Message { get; }
}