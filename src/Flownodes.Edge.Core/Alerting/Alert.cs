using Throw;

namespace Flownodes.Edge.Core.Alerting;

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

    public string Frn { get; init; }
    public AlertKind Kind { get; init; }
    public string Message { get; init; }

    public void Deconstruct(out string Frn, out AlertKind Kind, out string Message)
    {
        Frn = this.Frn;
        Kind = this.Kind;
        Message = this.Message;
    }
}