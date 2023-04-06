namespace Flownodes.Worker.Resourcing.Persistence;

[GenerateSerializer]
internal sealed record BehaviourId
{
    [Id(0)] public string? Value { get; set; }
}