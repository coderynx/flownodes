namespace Flownodes.Worker.Resourcing.Persistence;

internal interface IResourceGrainPersistenceEvent
{
}

internal sealed record InitializeResourceConfigurationEvent : IResourceGrainPersistenceEvent;

internal sealed record ClearResourceConfigurationEvent : IResourceGrainPersistenceEvent;

internal sealed record UpdateResourceConfigurationEvent
    (IDictionary<string, object?> Configuration) : IResourceGrainPersistenceEvent;

internal sealed record UpdateResourceMetadataEvent
    (IDictionary<string, string?> Metadata) : IResourceGrainPersistenceEvent;

internal sealed record ClearResourceMetadataEvent : IResourceGrainPersistenceEvent;

internal sealed record InitializeResourceStateEvent : IResourceGrainPersistenceEvent;

internal sealed record UpdateResourceStateEvent(IDictionary<string, object?> State) : IResourceGrainPersistenceEvent;

internal sealed record ClearResourceStateEvent : IResourceGrainPersistenceEvent;