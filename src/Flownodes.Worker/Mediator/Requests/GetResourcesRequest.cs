using Flownodes.Worker.Mediator.Responses;
using MediatR;

namespace Flownodes.Worker.Mediator.Requests;

public sealed record GetResourcesRequest(string TenantName) : IRequest<GetResourcesResponse>;