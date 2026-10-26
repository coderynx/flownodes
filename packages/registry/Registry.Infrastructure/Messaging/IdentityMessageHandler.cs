using Coderynx.MediatorKit.Abstractions;
using Core.Contracts.Identity;
using Registry.Application.Access.ApplyIdentityMessage;
using Wolverine.Attributes;

namespace Registry.Infrastructure.Messaging;

// HACK: public and documented only because Wolverine discovers handlers among public types; nothing calls it.
/// <summary>
/// Hands Identity's messages to the use case that records them (ADR-0026).
/// </summary>
[StickyHandler("registry-identity")]
public static class IdentityMessageHandler
{
    /// <summary>
    /// Handles a role granted in a Namespace.
    /// </summary>
    /// <param name="message">Identity's message.</param>
    /// <param name="sender">The Registry's mediator.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    /// <returns>A task that completes when the message is handled.</returns>
    public static Task Handle(RoleGranted message, ISender sender, CancellationToken cancellationToken) =>
        sender.SendAsync(new Command(message), cancellationToken);

    /// <summary>
    /// Handles a role revoked in a Namespace.
    /// </summary>
    /// <param name="message">Identity's message.</param>
    /// <param name="sender">The Registry's mediator.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    /// <returns>A task that completes when the message is handled.</returns>
    public static Task Handle(RoleRevoked message, ISender sender, CancellationToken cancellationToken) =>
        sender.SendAsync(new Command(message), cancellationToken);

    /// <summary>
    /// Handles a new Administrator.
    /// </summary>
    /// <param name="message">Identity's message.</param>
    /// <param name="sender">The Registry's mediator.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    /// <returns>A task that completes when the message is handled.</returns>
    public static Task Handle(AdministratorGranted message, ISender sender, CancellationToken cancellationToken) =>
        sender.SendAsync(new Command(message), cancellationToken);

    /// <summary>
    /// Handles an Administrator who no longer is one.
    /// </summary>
    /// <param name="message">Identity's message.</param>
    /// <param name="sender">The Registry's mediator.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    /// <returns>A task that completes when the message is handled.</returns>
    public static Task Handle(AdministratorRevoked message, ISender sender, CancellationToken cancellationToken) =>
        sender.SendAsync(new Command(message), cancellationToken);

    /// <summary>
    /// Handles a disabled User.
    /// </summary>
    /// <param name="message">Identity's message.</param>
    /// <param name="sender">The Registry's mediator.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    /// <returns>A task that completes when the message is handled.</returns>
    public static Task Handle(UserDisabled message, ISender sender, CancellationToken cancellationToken) =>
        sender.SendAsync(new Command(message), cancellationToken);

    /// <summary>
    /// Handles a User enabled again.
    /// </summary>
    /// <param name="message">Identity's message.</param>
    /// <param name="sender">The Registry's mediator.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    /// <returns>A task that completes when the message is handled.</returns>
    public static Task Handle(UserEnabled message, ISender sender, CancellationToken cancellationToken) =>
        sender.SendAsync(new Command(message), cancellationToken);

    /// <summary>
    /// Handles a deleted User.
    /// </summary>
    /// <param name="message">Identity's message.</param>
    /// <param name="sender">The Registry's mediator.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    /// <returns>A task that completes when the message is handled.</returns>
    public static Task Handle(UserDeleted message, ISender sender, CancellationToken cancellationToken) =>
        sender.SendAsync(new Command(message), cancellationToken);

    /// <summary>
    /// Handles a User's new username.
    /// </summary>
    /// <param name="message">Identity's message.</param>
    /// <param name="sender">The Registry's mediator.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    /// <returns>A task that completes when the message is handled.</returns>
    public static Task Handle(UserRenamed message, ISender sender, CancellationToken cancellationToken) =>
        sender.SendAsync(new Command(message), cancellationToken);
}
