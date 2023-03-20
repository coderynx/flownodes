using Flownodes.Worker.Mediator.Responses;
using MediatR;

namespace Flownodes.Worker.Mediator.Requests;

public record GetTenantRequest(string TenantName) : IRequest<GetTenantResponse>;