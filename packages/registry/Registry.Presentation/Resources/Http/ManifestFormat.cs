using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Registry.Presentation.Resources.Http;

internal static partial class ManifestFormat
{
    public const string Yaml = "application/yaml";
    private const int MaxNodes = 100_000;
    private static readonly string[] YamlTypes = [Yaml, "application/x-yaml", "text/yaml"];
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    public static async Task<(IReadOnlyList<JsonElement>? Documents, string? Problem)> ReadAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(request.Body);
        var text = await reader.ReadToEndAsync(cancellationToken);

        try
        {
            return (IsYaml(request.ContentType) ? FromYaml(text) : FromJson(text), null);
        }
        catch (Exception exception) when (exception is JsonException or YamlException or FormatException)
        {
            return (null, exception.Message);
        }
    }

    public static IResult Negotiate(HttpContext http, object body, int status = StatusCodes.Status200OK)
    {
        var accept = http.Request.GetTypedHeaders().Accept;
        var wantsYaml = accept.Any(type => YamlTypes.Contains(type.MediaType.Value, StringComparer.OrdinalIgnoreCase));

        return wantsYaml
            ? Results.Text(ToYaml(JsonSerializer.SerializeToNode(body, Web)), Yaml, statusCode: status)
            : Results.Json(body, Web, statusCode: status);
    }

    public static IResult Unreadable(string problem) =>
        Results.Problem(
            title: "manifest.unreadable",
            detail: $"The Manifest is neither valid YAML nor valid JSON: {problem}",
            statusCode: StatusCodes.Status400BadRequest);

    private static bool IsYaml(string? contentType) =>
        MediaTypeHeaderValue.TryParse(contentType, out var type) && YamlTypes.Contains(
            type.MediaType.Value,
            StringComparer.OrdinalIgnoreCase);

    private static List<JsonElement> FromJson(string text)
    {
        using var document = JsonDocument.Parse(text);
        var root = document.RootElement;

        return root.ValueKind == JsonValueKind.Array
            ? [.. root.EnumerateArray().Select(element => element.Clone())]
            : [root.Clone()];
    }

    private static List<JsonElement> FromYaml(string text)
    {
        var stream = new YamlStream();
        stream.Load(new StringReader(text));
        var budget = new[] { MaxNodes };

        return
        [
            .. stream.Documents.Where(document => !IsEmpty(document.RootNode))
                .Select(document => JsonSerializer.SerializeToElement(ToJson(document.RootNode, budget)))
        ];
    }

    private static bool IsEmpty(YamlNode node) =>
        node is YamlScalarNode { Style: ScalarStyle.Plain, Value: null or "" };

    private static JsonNode? ToJson(YamlNode node, int[] budget)
    {
        if (--budget[0] < 0)
        {
            throw new FormatException($"The Manifest expands to more than {MaxNodes} values.");
        }

        switch (node)
        {
            case YamlMappingNode mapping:
                var result = new JsonObject();

                foreach (var (key, value) in mapping.Children)
                {
                    var name = (key as YamlScalarNode)?.Value ?? throw new FormatException("A key must be a scalar.");
                    if (!result.TryAdd(name, ToJson(value, budget)))
                    {
                        throw new FormatException($"Key '{name}' appears twice.");
                    }
                }

                return result;
            case YamlSequenceNode sequence:
                return new JsonArray([.. sequence.Children.Select(child => ToJson(child, budget))]);
            case YamlScalarNode scalar:
                return Scalar(scalar);
            default:
                throw new FormatException("Unsupported YAML node.");
        }
    }

    private static JsonNode? Scalar(YamlScalarNode scalar)
    {
        var value = scalar.Value ?? string.Empty;
        if (scalar.Style != ScalarStyle.Plain)
        {
            return JsonValue.Create(value);
        }

        if (value is "" or "~" or "null" or "Null" or "NULL")
        {
            return null;
        }

        if (value is "true" or "True" or "TRUE" or "false" or "False" or "FALSE")
        {
            return JsonValue.Create(bool.Parse(value));
        }

        if (Number().IsMatch(value) && decimal.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var number))
        {
            return JsonValue.Create(number);
        }

        return JsonValue.Create(value);
    }

    private static string ToYaml(JsonNode? root)
    {
        var writer = new StringWriter();
        new YamlStream(new YamlDocument(ToYamlNode(root))).Save(writer, false);
        var yaml = writer.ToString();

        return yaml.EndsWith("...\n", StringComparison.Ordinal) ? yaml[..^4] : yaml;
    }

    private static YamlNode ToYamlNode(JsonNode? node) =>
        node switch
        {
            JsonObject json => new YamlMappingNode(
                json.Select(field => new KeyValuePair<YamlNode, YamlNode>(
                    new YamlScalarNode(field.Key),
                    ToYamlNode(field.Value)))),
            JsonArray array => new YamlSequenceNode(array.Select(ToYamlNode)),
            JsonValue value when value.GetValueKind() == JsonValueKind.String => StringNode(value.GetValue<string>()),
            JsonValue value => new YamlScalarNode(value.ToJsonString()) { Style = ScalarStyle.Plain },
            _ => new YamlScalarNode("null") { Style = ScalarStyle.Plain }
        };

    private static YamlScalarNode StringNode(string text) =>
        new(text)
        {
            Style = Scalar(new YamlScalarNode(text) { Style = ScalarStyle.Plain }) is JsonValue parsed &&
                    parsed.GetValueKind() == JsonValueKind.String
                ? ScalarStyle.Any
                : ScalarStyle.DoubleQuoted
        };

    [GeneratedRegex(@"^[-+]?(\.[0-9]+|[0-9]+(\.[0-9]*)?)([eE][-+]?[0-9]+)?$")]
    private static partial Regex Number();
}
