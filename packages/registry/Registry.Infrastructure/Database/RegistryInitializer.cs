using Coderynx.MediatorKit.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Registry.Application.Namespaces.SeedDefaultNamespace;

namespace Registry.Infrastructure.Database;

internal sealed class RegistryInitializer(IServiceScopeFactory scopeFactory) : IHostedLifecycleService
{
    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<RegistryDbContext>().Database.MigrateAsync(cancellationToken);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var result = await sender.SendAsync(new Command(), cancellationToken);
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Seeding the default Namespace failed: {result.Error.Message}");
        }
    }

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
