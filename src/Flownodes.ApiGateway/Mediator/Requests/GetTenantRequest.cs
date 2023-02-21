using Flownodes.ApiGateway.Mediator.Responses;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Requests;

public record GetTenantRequest(string TenantName) : IRequest<GetTenantResponse>;