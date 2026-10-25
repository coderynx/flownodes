using System.Text.Json;

namespace Engine.Infrastructure.Assets;

internal static class DataPointDeclarations
{
    public static IReadOnlyList<string> Names(JsonElement spec) =>
        spec.TryGetProperty("dataPoints", out var list) && list.ValueKind == JsonValueKind.Array
            ? [.. list.EnumerateArray().Select(dataPoint => dataPoint.GetProperty("name").GetString()!)]
            : [];
}
