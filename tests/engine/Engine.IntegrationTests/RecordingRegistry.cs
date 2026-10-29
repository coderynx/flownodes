using System.Collections.Concurrent;
using System.Text.Json;
using Core.Contracts.Resources;
using Engine.Infrastructure.Messaging;
using Engine.Infrastructure.Namespaces;

namespace Engine.IntegrationTests;

/// <summary>
/// Records what the Engine asks of the Registry and answers it with the Namespaces and Resources it was told to hold,
/// the way the Registry's messages reach the Engine.
/// </summary>
public sealed class RecordingRegistry(IGrainFactory grains) : IRegistryRequests
{
    private static readonly JsonElement Empty = JsonDocument.Parse("{}").RootElement;
    private readonly ConcurrentQueue<string> _resyncs = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<ResourceApplied>> _held = new();
    public IReadOnlyList<string> NamespaceResyncs => [.. _resyncs];

    public void Hold(string @namespace, string kind, string name, string spec, long seq) =>
        _held.GetOrAdd(@namespace, _ => new ConcurrentQueue<ResourceApplied>())
            .Enqueue(
                new ResourceApplied(
                    @namespace,
                    kind,
                    name,
                    EngineCluster.ResourceUid(kind, name),
                    1,
                    seq,
                    JsonDocument.Parse(spec).RootElement));

    public Task ListNamespacesAsync(CancellationToken cancellationToken)
    {
        var listed = new NamespacesListed([.. _held.Keys]);
        _ = Task.Run(() => NamespaceMessageHandler.Handle(listed, grains), CancellationToken.None);

        return Task.CompletedTask;
    }

    public Task ResyncNamespaceAsync(string name, CancellationToken cancellationToken)
    {
        _resyncs.Enqueue(name);
        if (_held.TryGetValue(name, out var resources))
        {
            _ = Task.Run(() => PublishAsync(name, [.. resources]), CancellationToken.None);
        }

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

    private async Task PublishAsync(string @namespace, IReadOnlyList<ResourceApplied> resources)
    {
        await NamespaceMessageHandler.Handle(
            new ResourceApplied(@namespace, "Namespace", @namespace, EngineCluster.NamespaceUid, 1, 1, Empty),
            grains);
        foreach (var resource in resources)
        {
            await NamespaceMessageHandler.Handle(resource, grains);
        }
    }
}
