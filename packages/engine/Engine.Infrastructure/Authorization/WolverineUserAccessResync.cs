using Core.Application.Authorization;
using Core.Contracts.Identity;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace Engine.Infrastructure.Authorization;

internal sealed class WolverineUserAccessResync(IServiceScopeFactory scopes) : IUserAccessResync
{
    public async Task RequestAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        await using var scope = scopes.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IMessageBus>().PublishAsync(new UsersResyncRequested(userId));
    }
}
