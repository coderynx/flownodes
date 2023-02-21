using Flownodes.ApiGateway.Mediator.Responses;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Requests;

public sealed record GetClusterInfoRequest : IRequest<GetClusterInfoResponse>;