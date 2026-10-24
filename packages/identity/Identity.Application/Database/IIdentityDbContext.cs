using Identity.Domain.Namespaces;
using Identity.Domain.Tokens;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Database;

/// <summary>
/// Identity's database as its command handlers see it: the unit of work a change commits through.
/// </summary>
public interface IIdentityDbContext
{
    /// <summary>
    /// Gets the Users, with their Role Grants.
    /// </summary>
    DbSet<User> Users { get; }
    /// <summary>
    /// Gets the Personal Access Tokens.
    /// </summary>
    DbSet<PersonalAccessToken> PersonalAccessTokens { get; }
    /// <summary>
    /// Gets the Namespaces the Registry announced.
    /// </summary>
    DbSet<KnownNamespace> KnownNamespaces { get; }

    /// <summary>
    /// Queues a message for the other modules. It is sent when, and only when, <see cref="SaveChangesAsync"/>
    /// commits the changes tracked beside it (ADR-0013).
    /// </summary>
    /// <param name="message">The message, a contract from <c>Core.Contracts</c>.</param>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <returns>A task that completes when the message is queued.</returns>
    ValueTask PublishAsync<TMessage>(TMessage message) where TMessage : notnull;

    /// <summary>
    /// Commits every change tracked since the last save and every message queued since then in one transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancels the commit.</param>
    /// <returns>A task that completes when the transaction has committed and its messages are on their way.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
