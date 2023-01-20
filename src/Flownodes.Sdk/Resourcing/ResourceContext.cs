namespace Flownodes.Sdk.Resourcing;

public record ResourceContext(ResourceConfiguration Configuration, ResourceMetadata Metadata,
    ResourceState State);