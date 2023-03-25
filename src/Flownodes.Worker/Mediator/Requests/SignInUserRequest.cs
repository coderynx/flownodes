using Flownodes.Worker.Mediator.Responses;
using MediatR;

namespace Flownodes.Worker.Mediator.Requests;

public sealed record SignInUserRequest(string Username, string Password) : IRequest<SignInUserResponse>;