using Throw;

namespace Flownodes.Cluster.Core.Alerting;

public record Alert
{
    public Alert(string frn, AlertKind kind, string message)
    {
        frn.Throw().IfWhiteSpace();
        message.Throw().IfWhiteSpace();

        Frn = frn;
        Kind = kind;
        Message = message;
    }

    public string Frn { get; }
    public AlertKind Kind { get; }
    public string Message { get; }
}