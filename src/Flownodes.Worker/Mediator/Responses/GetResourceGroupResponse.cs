using Flownodes.Sdk.Entities;

namespace Flownodes.Worker.Mediator.Responses;

public sealed record GetResourceGroupResponse : Response
{
    public GetResourceGroupResponse(EntityId id, Dictionary<string, object?> metadata,
        HashSet<EntityId> registrations)
    {
        Id = id;
        Metadata = metadata;
        Registrations = registrations;
    }

    public GetResourceGroupResponse(EntityId id, string message, ResponseKind responseKind) : base(message,
        responseKind)
    {
        Id = id;
    }

    public EntityId Id { get; }
    public Dictionary<string, object?> Metadata { get; } = new();
    public HashSet<EntityId> Registrations { get; } = new();
}