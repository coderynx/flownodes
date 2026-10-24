using Identity.Application.Database;
using Identity.Domain.Namespaces;
using Identity.Domain.Tokens;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;

namespace Identity.Infrastructure.Database;

internal sealed class IdentityUnitOfWork(IDbContextOutbox<IdentityDbContext> outbox) : IIdentityDbContext
{
    public DbSet<User> Users => outbox.DbContext.Users;
    public DbSet<PersonalAccessToken> PersonalAccessTokens => outbox.DbContext.PersonalAccessTokens;
    public DbSet<KnownNamespace> KnownNamespaces => outbox.DbContext.KnownNamespaces;

    public ValueTask PublishAsync<TMessage>(TMessage message) where TMessage : notnull =>
        new(outbox.PublishAsync(message).AsTask());

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        outbox.SaveChangesAndFlushMessagesAsync(cancellationToken);
}
