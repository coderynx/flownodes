using Flownodes.Worker.Mediator.Responses;
using MediatR;

namespace Flownodes.Worker.Mediator.Requests;

public sealed record CreateTenantRequest
    (string TenantName, IDictionary<string, string?>? Metadata = null) : IRequest<CreateTenantResponse>;