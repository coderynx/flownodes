using Flownodes.Sdk;

namespace Flownodes.Worker.Surrogates;

[GenerateSerializer]
internal struct FlownodesIdSurrogate
{
    public FlownodesIdSurrogate(FlownodesObject objectKind, string firstName, string? secondName = null)
    {
        ObjectKind = objectKind;
        FirstName = firstName;
        SecondName = secondName;
    }

    [Id(0)] public FlownodesObject ObjectKind { get; }

    [Id(1)] public string FirstName { get; }

    [Id(2)] public string? SecondName { get; }
}

[RegisterConverter]
internal sealed class FlownodesIdSurrogateConverter :
    IConverter<FlownodesId, FlownodesIdSurrogate>
{
    public FlownodesId ConvertFromSurrogate(in FlownodesIdSurrogate surrogate)
    {
        return new FlownodesId(surrogate.ObjectKind, surrogate.FirstName, surrogate.SecondName);
    }

    public FlownodesIdSurrogate ConvertToSurrogate(in FlownodesId value)
    {
        return new FlownodesIdSurrogate(value.ObjectKind, value.FirstName, value.SecondName);
    }
}