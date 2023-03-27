namespace Flownodes.Worker.Mediator.Responses;

public sealed record SignInUserResponse : Response
{
    public SignInUserResponse()
    {
    }

    public SignInUserResponse(string message, ResponseKind kind) : base(message, kind)
    {
    }
}