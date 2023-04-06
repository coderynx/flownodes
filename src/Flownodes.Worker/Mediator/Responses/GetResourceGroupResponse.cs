using Flownodes.Sdk.Entities;

namespace Flownodes.Worker.Mediator.Responses;

public sealed record GetResourceGroupResponse : Response
{
    public GetResourceGroupResponse(FlownodesId id, Dictionary<string, object?> metadata,
        HashSet<FlownodesId> registrations)
    {
        Id = id;
        Metadata = metadata;
        Registrations = registrations;
    }

    public GetResourceGroupResponse(FlownodesId id, string message, ResponseKind responseKind) : base(message,
        responseKind)
    {
        Id = id;
    }

    public FlownodesId Id { get; }
    public Dictionary<string, object?> Metadata { get; } = new();
    public HashSet<FlownodesId> Registrations { get; } = new();
}