using Flownodes.Shared.Eventing;

namespace Flownodes.Worker.Mediator.Responses;

public sealed record GetEventsResponse : Response
{
    public GetEventsResponse(string tenantName, HashSet<EventRegistration> events)
    {
        TenantName = tenantName;
        Events = events;
    }

    public GetEventsResponse(string tenantName, string message, ResponseKind kind) : base(message, kind)
    {
        TenantName = tenantName;
    }

    public string TenantName { get; }
    public HashSet<EventRegistration>? Events { get; }
}