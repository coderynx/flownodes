using Flownodes.ApiGateway.Mediator.Responses;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Requests;

public sealed record UpdateResourceStateRequest(string TenantName, string ResourceName,
    IDictionary<string, object?> State) : IRequest<UpdateResourceStateResponse>;