using Flownodes.ApiGateway.Mediator.Responses;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Requests;

public sealed record GetResourceRequest(string TenantName, string ResourceName) : IRequest<GetResourceResponse>;