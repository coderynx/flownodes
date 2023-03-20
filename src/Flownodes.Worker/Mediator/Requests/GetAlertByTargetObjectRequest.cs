using Flownodes.Worker.Mediator.Responses;
using MediatR;

namespace Flownodes.Worker.Mediator.Requests;

public record GetAlertByTargetObjectRequest
    (string TenantName, string TargetObjectName) : IRequest<GetAlertByTargetObjectResponse>;