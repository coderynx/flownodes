using Microsoft.EntityFrameworkCore;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;

namespace Registry.Application.Database;

/// <summary>
/// The Registry's database as its command handlers see it: the unit of work an apply commits through.
/// </summary>
public interface IApplicationDbContext
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
