using Coderynx.MediatorKit.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Registry.Application.Namespaces.PutNamespace;
using Registry.Domain.Namespaces;

namespace Registry.Infrastructure.Database;

internal sealed class RegistryInitializer(IServiceScopeFactory scopeFactory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<RegistryDbContext>().Database.MigrateAsync(cancellationToken);

        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var result = await sender.SendAsync(new Command(NamespaceName.Default), cancellationToken);
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Seeding the default Namespace failed: {result.Error.Message}");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
