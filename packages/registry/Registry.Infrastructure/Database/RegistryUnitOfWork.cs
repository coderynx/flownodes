using Core.Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Registry.Application.Database;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Wolverine.EntityFrameworkCore;

namespace Registry.Infrastructure.Database;

internal sealed class RegistryUnitOfWork(IDbContextOutbox<RegistryDbContext> outbox) : IApplicationDbContext
{
    public DbSet<Namespace> Namespaces => outbox.DbContext.Namespaces;
    public DbSet<Resource> Resources => outbox.DbContext.Resources;
    public DbSet<UserAccess> UserAccess => outbox.DbContext.UserAccess;
    public DbSet<Revision> Revisions => outbox.DbContext.Revisions;
    public DbSet<SecretValue> SecretValues => outbox.DbContext.SecretValues;

    public async Task LockNamespacesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default)
    {
        var database = outbox.DbContext.Database;
        if (database.CurrentTransaction is null)
        {
            await database.BeginTransactionAsync(cancellationToken);
        }

        foreach (var name in names)
        {
            var key = $"registry.namespace:{name}";
            await database.ExecuteSqlAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))",
                cancellationToken);
        }
    }

    public ValueTask PublishAsync<TMessage>(TMessage message) where TMessage : notnull =>
        new(outbox.PublishAsync(message).AsTask());

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
}
