using System.Collections.Concurrent;
using Core.Contracts.Resources;

namespace Registry.IntegrationTests;

public sealed class ResourceMessages
{
    private readonly ConcurrentQueue<object> _received = new();
    public IReadOnlyList<object> Received => [.. _received];

    public void Add(object message)
    {
        _received.Enqueue(message);
    }

    public async Task<ResourceApplied> WaitForAppliedAsync(string name, CancellationToken cancellationToken) =>
        await WaitForAsync<ResourceApplied>(applied => applied.Name == name, cancellationToken);

    public async Task<ResourceDeleted> WaitForDeletedAsync(string name, CancellationToken cancellationToken) =>
        await WaitForAsync<ResourceDeleted>(deleted => deleted.Name == name, cancellationToken);

    private async Task<TMessage> WaitForAsync<TMessage>(Func<TMessage, bool> match, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));

        while (true)
        {
            var found = _received.OfType<TMessage>().FirstOrDefault(match);
            if (found is not null)
            {
                return found;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token);
        }
    }
}

public static class ResourceMessageHandler
{
    public static void Handle(ResourceApplied message, ResourceMessages messages)
    {
        messages.Add(message);
    }

    public static void Handle(ResourceDeleted message, ResourceMessages messages)
    {
        messages.Add(message);
    }
}
