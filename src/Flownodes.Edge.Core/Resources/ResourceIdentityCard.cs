namespace Flownodes.Edge.Core.Resources;

[GenerateSerializer]
public record ResourceIdentityCard([property: Id(0)] string Frn, [property: Id(1)] string Id,
    [property: Id(2)] DateTime CreatedAt, [property: Id(3)] string BehaviorId, [property: Id(4)] DateTime? LastUpdate);