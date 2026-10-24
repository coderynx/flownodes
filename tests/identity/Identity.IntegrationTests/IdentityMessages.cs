using System.Collections.Concurrent;
using Core.Contracts.Identity;

namespace Identity.IntegrationTests;

public sealed class IdentityMessages
{
    private readonly ConcurrentQueue<object> _received = new();
    public IReadOnlyList<object> Received => [.. _received];

    public void Add(object message)
    {
        _received.Enqueue(message);
    }

    public async Task<TMessage> WaitForAsync<TMessage>(Func<TMessage, bool> match, CancellationToken cancellationToken)
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

public static class IdentityMessageHandler
{
    public static void Handle(RoleGranted message, IdentityMessages messages)
    {
        messages.Add(message);
    }

    public static void Handle(RoleRevoked message, IdentityMessages messages)
    {
        messages.Add(message);
    }

    public static void Handle(AdministratorGranted message, IdentityMessages messages)
    {
        messages.Add(message);
    }

    public static void Handle(AdministratorRevoked message, IdentityMessages messages)
    {
        messages.Add(message);
    }

    public static void Handle(UserDisabled message, IdentityMessages messages)
    {
        messages.Add(message);
    }

    public static void Handle(UserEnabled message, IdentityMessages messages)
    {
        messages.Add(message);
    }

    public static void Handle(UserDeleted message, IdentityMessages messages)
    {
        messages.Add(message);
    }

    public static void Handle(UserRenamed message, IdentityMessages messages)
    {
        messages.Add(message);
    }
}
