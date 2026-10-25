using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Flowctl;

internal static class Manifest
{
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

            return new StringContent(resources.ToJsonString(), Encoding.UTF8, MediaTypes.Json);
        }

        if (parts.Exists(part => part.IsJson))
        {
            throw new InvalidDataException("Give either YAML files or JSON files, not both.");
        }

        return new StringContent(
            string.Join(DocumentEnd, parts.Select(part => WithoutDocumentEnd(part.Text))),
            Encoding.UTF8,
            MediaTypes.Yaml);
    }

    private const string DocumentEnd = "\n...\n";

    private static string WithoutDocumentEnd(string yaml)
    {
        var trimmed = yaml.TrimEnd();

        return trimmed.EndsWith("\n...", StringComparison.Ordinal) ? trimmed[..^4] : trimmed;
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
