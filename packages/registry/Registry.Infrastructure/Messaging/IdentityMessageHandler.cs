using Coderynx.MediatorKit.Abstractions;
using Core.Contracts.Identity;
using Registry.Application.Access.ApplyIdentityMessage;

namespace Registry.Infrastructure.Messaging;

/// <summary>
/// Consumes Identity's messages about Users and Role Grants, which the Registry authorises every request from
/// (ADR-0026). It is public because Wolverine discovers handlers only among public types.
/// </summary>
public static class IdentityMessageHandler
{
    /// <summary>
    /// Records a role granted in a Namespace.
    /// </summary>
    /// <param name="message">Identity's message.</param>
    /// <param name="sender">The mediator the Registry's use cases run through.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    /// <returns>A task that completes when the message is handled.</returns>
    public static Task Handle(RoleGranted message, ISender sender, CancellationToken cancellationToken) =>
        sender.SendAsync(new Command(message), cancellationToken);

    /// <summary>
    /// Records a role revoked in a Namespace.
    /// </summary>
    /// <param name="message">Identity's message.</param>
    /// <param name="sender">The mediator the Registry's use cases run through.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    /// <returns>A task that completes when the message is handled.</returns>
    public static Task Handle(RoleRevoked message, ISender sender, CancellationToken cancellationToken) =>
        sender.SendAsync(new Command(message), cancellationToken);

    /// <summary>
    /// Records a new Administrator.
    /// </summary>
    /// <param name="message">Identity's message.</param>
    /// <param name="sender">The mediator the Registry's use cases run through.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    /// <returns>A task that completes when the message is handled.</returns>
    public static Task Handle(AdministratorGranted message, ISender sender, CancellationToken cancellationToken) =>
        sender.SendAsync(new Command(message), cancellationToken);

    /// <summary>
    /// Records an Administrator who no longer is one.
    /// </summary>
    /// <param name="message">Identity's message.</param>
    /// <param name="sender">The mediator the Registry's use cases run through.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    /// <returns>A task that completes when the message is handled.</returns>
    public static Task Handle(AdministratorRevoked message, ISender sender, CancellationToken cancellationToken) =>
        sender.SendAsync(new Command(message), cancellationToken);

    /// <summary>
    /// Records a disabled User.
    /// </summary>
    /// <param name="message">Identity's message.</param>
    /// <param name="sender">The mediator the Registry's use cases run through.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    /// <returns>A task that completes when the message is handled.</returns>
    public static Task Handle(UserDisabled message, ISender sender, CancellationToken cancellationToken) =>
        sender.SendAsync(new Command(message), cancellationToken);

    /// <summary>
    /// Records a User enabled again.
    /// </summary>
    /// <param name="message">Identity's message.</param>
    /// <param name="sender">The mediator the Registry's use cases run through.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    /// <returns>A task that completes when the message is handled.</returns>
    public static Task Handle(UserEnabled message, ISender sender, CancellationToken cancellationToken) =>
        sender.SendAsync(new Command(message), cancellationToken);

    /// <summary>
    /// Records a deleted User.
    /// </summary>
    /// <param name="message">Identity's message.</param>
    /// <param name="sender">The mediator the Registry's use cases run through.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    /// <returns>A task that completes when the message is handled.</returns>
    public static Task Handle(UserDeleted message, ISender sender, CancellationToken cancellationToken) =>
        sender.SendAsync(new Command(message), cancellationToken);

    /// <summary>
    /// Records a User's new username.
    /// </summary>
    /// <param name="message">Identity's message.</param>
    /// <param name="sender">The mediator the Registry's use cases run through.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    /// <returns>A task that completes when the message is handled.</returns>
    public static Task Handle(UserRenamed message, ISender sender, CancellationToken cancellationToken) =>
        sender.SendAsync(new Command(message), cancellationToken);
}
