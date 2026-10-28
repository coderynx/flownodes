using System.Collections.Concurrent;
using System.Text.Json;
using Engine.Infrastructure.Resources;

namespace Engine.IntegrationTests;

public sealed class RecordingStatus : IStatusReporter
{
    private readonly ConcurrentDictionary<string, (long Seq, JsonElement Status)> _latest = new();
    private readonly ConcurrentQueue<(string Key, long Seq, long Generation, JsonElement Status)> _reports = new();

    public Task ReportAsync(
        string @namespace,
        string kind,
        string name,
        Guid uid,
        long observedGeneration,
        long statusSeq,
        JsonElement status)
    {
        var key = $"{@namespace}/{kind}/{name}";
        var report = status.Clone();
        _reports.Enqueue((key, statusSeq, observedGeneration, report));
        _latest.AddOrUpdate(key, (statusSeq, report), (_, held) => statusSeq > held.Seq ? (statusSeq, report) : held);

        return Task.CompletedTask;
    }

    public JsonElement? Latest(string @namespace, string kind, string name) =>
        _latest.TryGetValue($"{@namespace}/{kind}/{name}", out var held) ? held.Status : null;

    public IReadOnlyList<(long Seq, long Generation)> Reports(string @namespace, string kind, string name) =>
        _reports.Where(report => report.Key == $"{@namespace}/{kind}/{name}")
            .Select(report => (report.Seq, report.Generation))
            .ToList();

    public Task<JsonElement> WaitForAsync(
        string @namespace,
        string kind,
        string name,
        Func<JsonElement, bool> condition,
        CancellationToken cancellationToken) =>
        Wait.UntilValueAsync<JsonElement>(
            () => Latest(@namespace, kind, name) is { } status && condition(status) ? status : null,
            cancellationToken);

    public static string Reason(JsonElement status) =>
        status.GetProperty("conditions")[0].GetProperty("reason").GetString()!;
}

public static class Wait
{
    public static async Task<T> UntilValueAsync<T>(Func<T?> probe, CancellationToken cancellationToken) where T : struct
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        T? found;
        while ((found = probe()) is null)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(25), timeout.Token);
        }

        return found.Value;
    }

    public static async Task<T> UntilAsync<T>(Func<Task<T?>> probe, CancellationToken cancellationToken) where T : class
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        T? found;
        while ((found = await probe()) is null)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(25), timeout.Token);
        }

        return found;
    }
}
