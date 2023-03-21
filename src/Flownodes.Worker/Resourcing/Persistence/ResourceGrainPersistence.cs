using Flownodes.Worker.Extensions;

namespace Flownodes.Worker.Resourcing.Persistence;

[GenerateSerializer]
internal sealed record ResourceGrainPersistence
{
    [Id(0)] public DateTime CreatedAtDate { get; } = DateTime.Now;

    [Id(1)] public Dictionary<string, string?> Metadata { get; } = new();
    [Id(2)] public DateTime? LastMetadataUpdateDate { get; private set; }

    [Id(3)] public Dictionary<string, object?>? Configuration { get; private set; }
    [Id(4)] public DateTime? LastConfigurationUpdateDate { get; private set; }

    [Id(5)] public Dictionary<string, object?>? State { get; private set; }
    [Id(6)] public DateTime? LastStateUpdateDate { get; private set; }

    public void InitializeConfiguration()
    {
        Configuration = new Dictionary<string, object?>();
    }

    public void UpdateConfiguration(IDictionary<string, object?> configuration)
    {
        Configuration ??= new Dictionary<string, object?>();
        Configuration.MergeInPlace(configuration);
        LastConfigurationUpdateDate = DateTime.Now;
    }

    public void ClearConfiguration()
    {
        Configuration?.Clear();
        LastConfigurationUpdateDate = DateTime.Now;
    }

    public void UpdateMetadata(IDictionary<string, string?> metadata)
    {
        Metadata.MergeInPlace(metadata);
        LastMetadataUpdateDate = DateTime.Now;
    }

    public void ClearMetadata()
    {
        Metadata.Clear();
        LastMetadataUpdateDate = DateTime.Now;
    }

    public void InitializeState()
    {
        State = new Dictionary<string, object?>();
    }

    public void UpdateState(IDictionary<string, object?> state)
    {
        State ??= new Dictionary<string, object?>();
        State.MergeInPlace(state);
        LastStateUpdateDate = DateTime.Now;
    }

    public void ClearState()
    {
        State?.Clear();
        LastStateUpdateDate = DateTime.Now;
    }
}