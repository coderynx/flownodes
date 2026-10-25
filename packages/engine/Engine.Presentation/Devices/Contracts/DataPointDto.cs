using System.Text.Json;
using Engine.Application.Devices;

namespace Engine.Presentation.Devices.Contracts;

internal sealed record DataPointDto(
    string Name,
    string Type,
    bool Writable,
    string? Unit,
    double? Min,
    double? Max,
    IReadOnlyList<string>? Values,
    JsonElement? Value,
    string Quality,
    DateTimeOffset? SourceTimestamp);

internal static class DataPointMappings
{
    public static DataPointDto ToDto(this DataPointReading reading) =>
        new(
            reading.Name,
            reading.Type,
            reading.Writable,
            reading.Unit,
            reading.Min,
            reading.Max,
            reading.Values,
            reading.Value,
            reading.Quality,
            reading.SourceTimestamp);
}
