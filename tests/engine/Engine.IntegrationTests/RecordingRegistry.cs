using System.Collections.Concurrent;
using Engine.Infrastructure.Namespaces;

namespace Engine.IntegrationTests;

public sealed class RecordingRegistry : IRegistryRequests
{
    private readonly ConcurrentQueue<string> _resyncs = new();
    public IReadOnlyList<string> NamespaceResyncs => [.. _resyncs];

    public Task ResyncNamespaceAsync(string name, CancellationToken cancellationToken)
    {
        _resyncs.Enqueue(name);

        return Task.CompletedTask;
    }

    public async Task WaitForResyncsAsync(string name, int count, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));

        while (_resyncs.Count(resync => resync == name) < count)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50), timeout.Token);
        }
    }

    public int Resyncs(string name) => _resyncs.Count(resync => resync == name);
}
