namespace Flownodes.Sdk.Resourcing;

public record ResourceContext(ActualResourceConfiguration Configuration, Dictionary<string, string?> Metadata,
    ActualResourceState State);