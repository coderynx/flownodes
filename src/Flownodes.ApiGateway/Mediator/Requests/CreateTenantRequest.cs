using Flownodes.ApiGateway.Mediator.Responses;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Requests;

public sealed record CreateTenantRequest
    (string TenantName, IDictionary<string, string?>? Metadata = null) : IRequest<CreateTenantResponse>;