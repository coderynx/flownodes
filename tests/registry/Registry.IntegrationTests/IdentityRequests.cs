using System.Collections.Concurrent;
using Core.Contracts.Identity;

namespace Registry.IntegrationTests;

public sealed class IdentityRequests
{
    private readonly ConcurrentQueue<UsersResyncRequested> _received = new();
    public IReadOnlyList<UsersResyncRequested> Received => [.. _received];

    public void Add(UsersResyncRequested request)
    {
        _received.Enqueue(request);
    }

    public async Task WaitForAsync(Guid? userId, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));

        while (!_received.Any(request => request.UserId == userId))
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token);
        }
    }
}

public static class IdentityRequestHandler
{
    public static void Handle(UsersResyncRequested message, IdentityRequests requests)
    {
        requests.Add(message);
    }
}
