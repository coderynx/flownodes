namespace Flownodes.Worker.Resourcing.Persistence;

[GenerateSerializer]
internal sealed record BehaviourId
{
    public BehaviourId(string value)
    {
        Value = value;
    }

    public BehaviourId() { }

    [Id(0)] public string? Value { get; set; }
}