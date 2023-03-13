using Flownodes.Sdk.Alerting;

namespace Flownodes.ApiGateway.Mediator.Responses;

public record GetAlertByTargetObjectResponse : Response
{
    public GetAlertByTargetObjectResponse(string tenantName, string alertName, string targetObjectName,
        AlertSeverity severity, string description, DateTime firedAt)
    {
        TenantName = tenantName;
        AlertName = alertName;
        TargetObjectName = targetObjectName;
        Severity = severity;
        Description = description;
        FiredAt = firedAt;
    }

    public GetAlertByTargetObjectResponse(string tenantName, string targetObjectName, string message,
        ResponseKind responseKind) : base(message, responseKind)
    {
        TenantName = tenantName;
        TargetObjectName = targetObjectName;
    }

    public string TenantName { get; }
    public string? AlertName { get; }
    public string TargetObjectName { get; }
    public AlertSeverity? Severity { get; }
    public string? Description { get; }
    public DateTime? FiredAt { get; }
}