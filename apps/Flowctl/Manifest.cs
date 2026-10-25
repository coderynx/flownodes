using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Flowctl;

internal static class Manifest
{
    private const string Yaml = "application/yaml";
    private const string Json = "application/json";

    public static async Task<StringContent> ReadAsync(
        IReadOnlyList<string> files,
        TextReader input,
        CancellationToken cancellationToken)
    {
        var parts = new List<(string Text, bool IsJson)>();

        foreach (var file in files)
        {
            var text = file == "-"
                ? await input.ReadToEndAsync(cancellationToken)
                : await File.ReadAllTextAsync(file, cancellationToken);
            parts.Add((text, file.EndsWith(".json", StringComparison.OrdinalIgnoreCase)));
        }

        if (parts.TrueForAll(part => part.IsJson))
        {
            var resources = new JsonArray();

            foreach (var node in parts.Select(part => JsonNode.Parse(part.Text)))
            {
                foreach (var resource in node is JsonArray array ? [.. array] : new[] { node })
                {
                    resources.Add(resource?.DeepClone());
                }
            }

            return new StringContent(resources.ToJsonString(), Encoding.UTF8, Json);
        }

        if (parts.Exists(part => part.IsJson))
        {
            throw new InvalidDataException("Give either YAML files or JSON files, not both.");
        }

        return new StringContent(string.Join("\n---\n", parts.Select(part => part.Text)), Encoding.UTF8, Yaml);
    }

    public static bool IsJson(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
