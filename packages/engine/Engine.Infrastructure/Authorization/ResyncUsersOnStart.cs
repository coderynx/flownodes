using Core.Application.Authorization;
using Microsoft.Extensions.Hosting;

namespace Engine.Infrastructure.Authorization;

internal sealed class ResyncUsersOnStart(IUserAccessResync resync) : IHostedLifecycleService
{
    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StartedAsync(CancellationToken cancellationToken) => resync.RequestAsync(null, cancellationToken);
    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
