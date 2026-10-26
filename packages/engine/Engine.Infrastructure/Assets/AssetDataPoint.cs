using System.Text.Json;

namespace Engine.Infrastructure.Assets;

internal sealed record AssetDataPoint(
    string Name,
    string Type,
    bool Writable,
    string? Unit,
    double? Min,
    double? Max,
    IReadOnlyList<string>? Values,
    DataPointSource? Source)
{
    public static IReadOnlyList<AssetDataPoint> Merge(IReadOnlyList<string> inherited, JsonElement spec)
    {
        var sources = spec.TryGetProperty("sources", out var given) && given.ValueKind == JsonValueKind.Object
            ? given.EnumerateObject().ToDictionary(entry => entry.Name, entry => entry.Value)
            : [];
        var declarations = inherited.Select(text => JsonDocument.Parse(text).RootElement).Concat(Declarations(spec));

        return
        [
            .. declarations.Select(declaration =>
            {
                var name = declaration.GetProperty("name").GetString()!;
                JsonElement? source = declaration.TryGetProperty("source", out var declared) ? declared : null;
                if (sources.TryGetValue(name, out var overridden))
                {
                    source = overridden;
                }

                return From(declaration, source);
            })
        ];
    }

    public static IReadOnlyList<string> DeclarationTexts(JsonElement spec) =>
        [.. Declarations(spec).Select(declaration => declaration.GetRawText())];

    private static IEnumerable<JsonElement> Declarations(JsonElement spec) =>
        spec.TryGetProperty("dataPoints", out var list) && list.ValueKind == JsonValueKind.Array
            ? list.EnumerateArray()
            : [];

    private static AssetDataPoint From(JsonElement declaration, JsonElement? source)
    {
        var mapped = DataPointSource.Parse(source);
        var writable = mapped switch
        {
            null => true,
            { Device: not null } => declaration.TryGetProperty("writable", out var flag) &&
                                    flag.ValueKind == JsonValueKind.True,
            _ => false
        };

        return new AssetDataPoint(
            declaration.GetProperty("name").GetString()!,
            declaration.GetProperty("type").GetString()!,
            writable,
            declaration.TryGetProperty("unit", out var unit) ? unit.GetString() : null,
            Number(declaration, "min"),
            Number(declaration, "max"),
            declaration.TryGetProperty("values", out var values)
                ? [.. values.EnumerateArray().Select(value => value.GetString()!)]
                : null,
            mapped);
    }

    private static double? Number(JsonElement declaration, string name) =>
        declaration.TryGetProperty(name, out var value) ? value.GetDouble() : null;
}

internal sealed record DataPointSource(
    string? Device,
    string? DeviceDataPoint,
    string? Operation,
    IReadOnlyList<string> Inputs)
{
    public static DataPointSource? Parse(JsonElement? source)
    {
        if (source is not { ValueKind: JsonValueKind.Object } value)
        {
            return null;
        }

        if (value.TryGetProperty("compute", out var compute))
        {
            return new DataPointSource(
                null,
                null,
                compute.GetProperty("op").GetString(),
                [.. compute.GetProperty("inputs").EnumerateArray().Select(input => input.GetString()!)]);
        }

        return new DataPointSource(
            value.GetProperty("device").GetString(),
            value.GetProperty("dataPoint").GetString(),
            null,
            []);
    }
}
