using Core.Application.Authorization;
using Microsoft.EntityFrameworkCore;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;

namespace Registry.Application.Database;

/// <summary>
/// The Registry's database as its command handlers see it: the unit of work an apply commits through, and the replica
/// of Users and Role Grants that authorises every request.
/// </summary>
public interface IApplicationDbContext : IUserAccessSource
{
    /// <summary>
    /// Gets the Namespaces the Registry stores.
    /// </summary>
    DbSet<Namespace> Namespaces { get; }
    /// <summary>
    /// Gets the namespaced Resources the Registry stores.
    /// </summary>
    DbSet<Resource> Resources { get; }
    /// <summary>
    /// Gets the Revisions of every Resource and Namespace, tombstones included.
    /// </summary>
    DbSet<Revision> Revisions { get; }

    /// <summary>
    /// Starts the transaction <see cref="SaveChangesAsync"/> commits and, inside it, waits for every other apply or
    /// delete in the given Namespaces to finish, so validation sees a consistent Namespace (ADR-0007). The locks are
    /// released when the transaction commits or the unit of work is disposed.
    /// </summary>
    /// <param name="names">The names of the Namespaces, in ordinal order so two callers never deadlock.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns>A task that completes when the caller holds every lock.</returns>
    Task LockNamespacesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default);

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
    new Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
