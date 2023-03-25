namespace Flownodes.Worker.Mediator.Responses;

public sealed record CreateUserResponse : Response
{
    public CreateUserResponse(string username, string email)
    {
        Username = username;
        Email = email;
    }

    public CreateUserResponse(string username, string email, string message, ResponseKind responseKind)
        : base(message, responseKind)
    {
        Username = username;
        Email = email;
    }

    public string Username { get; }
    public string Email { get; }
}