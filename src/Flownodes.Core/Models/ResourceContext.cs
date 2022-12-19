namespace Flownodes.Core.Models;

public record ResourceContext(ResourceConfiguration? Configuration, Dictionary<string, string> Metadata,
    ResourceState? State);