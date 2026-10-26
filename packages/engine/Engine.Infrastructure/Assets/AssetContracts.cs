namespace Engine.Infrastructure.Assets;

[GenerateSerializer]
[Immutable]
internal sealed record AssetTypeInfo(
    [property: Id(0)] bool Exists,
    [property: Id(1)] IReadOnlyList<string> DataPoints,
    [property: Id(2)] IReadOnlyList<string> Alarms);

[GenerateSerializer]
internal sealed class AssetValues
{
    [Id(0)] public Dictionary<string, WrittenValue> Written { get; set; } = [];
}

[GenerateSerializer]
[Immutable]
internal sealed record WrittenValue([property: Id(0)] string Value, [property: Id(1)] DateTimeOffset Timestamp);
