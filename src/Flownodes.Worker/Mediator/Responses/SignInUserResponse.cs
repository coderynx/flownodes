using Microsoft.AspNetCore.Identity;

namespace Flownodes.Worker.Mediator.Responses;

public sealed record SignInUserResponse : Response
{
    public SignInUserResponse(SignInResult result)
    {
        Result = result;
    }

    public SignInUserResponse(string message, ResponseKind kind) : base(message, kind)
    {
    }
    
    public SignInResult? Result { get; }
}