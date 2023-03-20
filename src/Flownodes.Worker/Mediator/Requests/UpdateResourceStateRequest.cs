using Flownodes.Worker.Mediator.Responses;
using MediatR;

namespace Flownodes.Worker.Mediator.Requests;

public sealed record UpdateResourceStateRequest(string TenantName, string ResourceName,
    Dictionary<string, object?> State) : IRequest<UpdateResourceStateResponse>;