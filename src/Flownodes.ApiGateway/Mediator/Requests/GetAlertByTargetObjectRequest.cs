using Flownodes.ApiGateway.Mediator.Responses;
using MediatR;

namespace Flownodes.ApiGateway.Mediator.Requests;

public record GetAlertByTargetObjectRequest
    (string TenantName, string TargetObjectName) : IRequest<GetAlertByTargetObjectResponse>;