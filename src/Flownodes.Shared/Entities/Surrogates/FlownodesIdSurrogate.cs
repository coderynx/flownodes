using Flownodes.Sdk.Entities;

namespace Flownodes.Shared.Entities.Surrogates;

[GenerateSerializer]
internal struct FlownodesIdSurrogate
{
    public FlownodesIdSurrogate(FlownodesEntity entityKind, string firstName, string? secondName = null)
    {
        EntityKind = entityKind;
        FirstName = firstName;
        SecondName = secondName;
    }

    [Id(0)] public FlownodesEntity EntityKind { get; }

    [Id(1)] public string FirstName { get; }

    [Id(2)] public string? SecondName { get; }
}

[RegisterConverter]
internal sealed class FlownodesIdSurrogateConverter : IConverter<FlownodesId, FlownodesIdSurrogate>
{
    public FlownodesId ConvertFromSurrogate(in FlownodesIdSurrogate surrogate)
    {
        return new FlownodesId(surrogate.EntityKind, surrogate.FirstName, surrogate.SecondName);
    }

    public FlownodesIdSurrogate ConvertToSurrogate(in FlownodesId value)
    {
        return new FlownodesIdSurrogate(value.EntityKind, value.FirstName, value.SecondName);
    }
}