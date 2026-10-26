namespace Engine.Infrastructure.Devices;

[GenerateSerializer]
[Immutable]
internal sealed record DataPointSnapshot(
    [property: Id(0)] string Name,
    [property: Id(1)] string Type,
    [property: Id(2)] bool Writable,
    [property: Id(3)] string? Unit,
    [property: Id(4)] double? Min,
    [property: Id(5)] double? Max,
    [property: Id(6)] IReadOnlyList<string>? Values,
    [property: Id(7)] string? Value,
    [property: Id(8)] int Quality,
    [property: Id(9)] DateTimeOffset? Timestamp);

[GenerateSerializer]
[Immutable]
internal sealed record DeviceFeed(
    [property: Id(0)] bool Exists,
    [property: Id(1)] bool SurfaceKnown,
    [property: Id(2)] IReadOnlyList<DataPointSnapshot> DataPoints);
