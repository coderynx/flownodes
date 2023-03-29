namespace Flownodes.Worker.Authentication;

public sealed record AdminSecret
{
    public string Secret { get; set; } = "secret";
}