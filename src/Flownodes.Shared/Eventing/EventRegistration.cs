using Flownodes.Sdk.Entities;

namespace Flownodes.Shared.Eventing;

[GenerateSerializer]
public enum EventKind
{
    DeployedResource,
    RemovedResource,
    UpdatedResource
}

[GenerateSerializer]
public sealed record EventRegistration([property: Id(0)] DateTime RegisteredAt, [property: Id(1)] EventKind Kind,
    [property: Id(2)] FlownodesId TargetEntity);