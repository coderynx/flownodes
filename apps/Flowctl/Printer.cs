using System.Text.Json;

namespace Flowctl;

internal sealed class Printer(CliContext context)
{
    public async Task<int> ProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!Manifest.IsJson(text))
        {
            await context.Error.WriteLineAsync($"error: the server answered {(int)response.StatusCode}.");

            return 1;
        }

        using var problem = JsonDocument.Parse(text);
        var root = problem.RootElement;
        var detail = Text(root, "detail") ?? Text(root, "title") ?? $"the server answered {(int)response.StatusCode}";
        await context.Error.WriteLineAsync($"error: {detail}");

        if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
        {
            foreach (var error in errors.EnumerateArray())
            {
                var subject = Text(error, "kind") is { } kind ? $"{kind}/{Text(error, "name") ?? "?"}" : "?";
                var document = error.GetProperty("document");
                var where = document.ValueKind == JsonValueKind.Number
                    ? $"document {document.GetInt32()}"
                    : "not in the Manifest";
                await context.Error.WriteLineAsync(
                    $"  {where} {subject} {Text(error, "path")}: {Text(error, "message")}");
            }
        }

        return 1;
    }

    public async Task ChangesAsync(JsonElement body, bool withChanges)
    {
        var dryRun = body.GetProperty("dryRun").GetBoolean() ? " (dry run)" : string.Empty;

        foreach (var resource in body.GetProperty("resources").EnumerateArray())
        {
            var action = Text(resource, "action") ?? "deleted";
            await context.Output.WriteLineAsync($"{Subject(resource)} {action}{dryRun}");
            if (!withChanges || !resource.TryGetProperty("changes", out var changes))
            {
                continue;
            }

            foreach (var change in changes.EnumerateArray())
            {
                await context.Output.WriteLineAsync(Change(change));
            }
        }
    }

    public Task TableAsync(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var widths = headers
            .Select((header, column) => rows.Select(row => row[column].Length).Append(header.Length).Max())
            .ToList();
        var lines = rows.Prepend(headers)
            .Select(row => string.Join("   ", row.Select((cell, column) => cell.PadRight(widths[column]))).TrimEnd());

        return context.Output.WriteLineAsync(string.Join(Environment.NewLine, lines));
    }

    public static string? Text(JsonElement element, string field) =>
        element.TryGetProperty(field, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string Subject(JsonElement resource) =>
        $"{Text(resource, "kind")!.ToLowerInvariant()}/{Text(resource, "name")}";

    private static string Change(JsonElement change)
    {
        var path = Text(change, "path");
        var from = Value(change, "from");
        var to = Value(change, "to");

        return Text(change, "op") switch
        {
            "added" => $"  + {path}: {to}",
            "removed" => $"  - {path}: {from}",
            _ => $"  ~ {path}: {from} -> {to}"
        };
    }

    private static string Value(JsonElement change, string field) =>
        change.TryGetProperty(field, out var value) ? value.GetRawText() : string.Empty;
}
