using Coderynx.MediatorKit.Abstractions;
using Core.Contracts.Identity;
using Identity.Application.Users.ResyncUsers;

namespace Identity.Infrastructure.Messaging;

// HACK: public and documented only because Wolverine discovers handlers among public types; nothing calls it.
/// <summary>
/// Hands the other modules' requests about Users to the use cases that answer them (ADR-0026).
/// </summary>
public static class UserMessageHandler
{
    /// <summary>
    /// Handles a request to publish Users again.
    /// </summary>
    /// <param name="message">The module's message.</param>
    /// <param name="sender">Identity's mediator.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    /// <returns>A task that completes when the message is handled.</returns>
    public static Task Handle(UsersResyncRequested message, ISender sender, CancellationToken cancellationToken) =>
        sender.SendAsync(new Command(message.UserId), cancellationToken);
}
