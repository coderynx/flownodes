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

    public ValueTask PublishAsync<TMessage>(TMessage message) where TMessage : notnull =>
        new(outbox.PublishAsync(message).AsTask());

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
}
