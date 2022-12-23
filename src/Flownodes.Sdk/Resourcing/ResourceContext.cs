namespace Flownodes.Sdk.Resourcing;

public record ResourceContext(ResourceConfiguration Configuration, Dictionary<string, string?> Metadata,
    ResourceState State);