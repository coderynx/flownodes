using Flownodes.Worker.Mediator.Responses;
using MediatR;

namespace Flownodes.Worker.Mediator.Requests;

public class CreateApiKeyRequest : IRequest<CreateApiKeyResponse>
{
    public CreateApiKeyRequest(string username, string name)
    {
        Username = username;
        Name = name;
    }

    public string Username { get; }
    public string Name { get; }
}