using Flownodes.Worker.Mediator.Responses;
using MediatR;

namespace Flownodes.Worker.Mediator.Requests;

public sealed record GetResourceRequest(string TenantName, string ResourceName) : IRequest<GetResourceResponse>;