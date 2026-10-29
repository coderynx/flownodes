using System.Collections.Concurrent;
using Core.Application.Authorization;

namespace Engine.IntegrationTests;

public sealed class RecordingUserAccessResync : IUserAccessResync
{
    private readonly ConcurrentQueue<Guid?> _requests = new();
    public IReadOnlyList<Guid?> Requests => [.. _requests];

    public Task RequestAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        _requests.Enqueue(userId);

        return Task.CompletedTask;
    }
}
