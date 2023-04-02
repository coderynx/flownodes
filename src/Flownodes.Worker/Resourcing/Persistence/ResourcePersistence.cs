namespace Flownodes.Worker.Resourcing.Persistence;

[GenerateSerializer]
internal sealed record ResourceMetadata
{
    [Id(0)] public Dictionary<string, string?> Properties { get; set; } = new();
    [Id(1)] public DateTime CreatedAt { get; } = DateTime.Now;
}

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