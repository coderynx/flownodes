using Flownodes.Worker.Mediator.Responses;
using MediatR;

namespace Flownodes.Worker.Mediator.Requests;

public sealed record GetEventsRequest(string TenantName) : IRequest<GetEventsResponse>;