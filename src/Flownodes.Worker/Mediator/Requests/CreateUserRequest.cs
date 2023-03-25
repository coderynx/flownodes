using Flownodes.Worker.Mediator.Responses;
using MediatR;

namespace Flownodes.Worker.Mediator.Requests;

public sealed record CreateUserRequest(string Username, string Email, string Password) : IRequest<CreateUserResponse>;