using Core.Contracts.Identity;
using Core.Domain.Authorization;
using Engine.Infrastructure.Authorization;
using Wolverine.Attributes;

namespace Engine.Infrastructure.Messaging;

// HACK: public and documented only because Wolverine discovers handlers among public types; nothing calls it.
/// <summary>
/// Hands Identity's messages about Role Grants, disabled or deleted Users and resynced Users to the User's grain
/// (ADR-0026).
/// </summary>
[StickyHandler("engine-identity")]
public static class IdentityMessageHandler
{
    /// <summary>
    /// Handles a role granted in a Namespace.
    /// </summary>
    /// <param name="message">Identity's message.</param>
    /// <param name="grains">Orleans' grain factory.</param>
    /// <returns>A task that completes when the grain has handled the message.</returns>
    public static Task Handle(RoleGranted message, IGrainFactory grains) =>
        grains.GetGrain<IUserAccessGrain>(message.UserId)
            .GrantRoleAsync(message.Namespace, Enum.Parse<NamespaceRole>(message.Role), message.Seq);

    /// <summary>
    /// Handles a role revoked in a Namespace.
    /// </summary>
    /// <param name="message">Identity's message.</param>
    /// <param name="grains">Orleans' grain factory.</param>
    /// <returns>A task that completes when the grain has handled the message.</returns>
    public static Task Handle(RoleRevoked message, IGrainFactory grains) =>
        grains.GetGrain<IUserAccessGrain>(message.UserId)
            .RevokeRoleAsync(message.Namespace, Enum.Parse<NamespaceRole>(message.Role), message.Seq);

    /// <summary>
    /// Handles a disabled User.
    /// </summary>
    /// <param name="message">Identity's message.</param>
    /// <param name="grains">Orleans' grain factory.</param>
    /// <returns>A task that completes when the grain has handled the message.</returns>
    public static Task Handle(UserDisabled message, IGrainFactory grains) =>
        grains.GetGrain<IUserAccessGrain>(message.UserId).DisableAsync(message.Seq);

    /// <summary>
    /// Handles a User enabled again.
    /// </summary>
    /// <param name="message">Identity's message.</param>
    /// <param name="grains">Orleans' grain factory.</param>
    /// <returns>A task that completes when the grain has handled the message.</returns>
    public static Task Handle(UserEnabled message, IGrainFactory grains) =>
        grains.GetGrain<IUserAccessGrain>(message.UserId).EnableAsync(message.Seq);

    /// <summary>
    /// Handles a deleted User.
    /// </summary>
    /// <param name="message">Identity's message.</param>
    /// <param name="grains">Orleans' grain factory.</param>
    /// <returns>A task that completes when the grain has handled the message.</returns>
    public static Task Handle(UserDeleted message, IGrainFactory grains) =>
        grains.GetGrain<IUserAccessGrain>(message.UserId).DeleteAsync(message.Seq);

    /// <summary>
    /// Handles the whole state of a User, published on a resync.
    /// </summary>
    /// <param name="message">Identity's message.</param>
    /// <param name="grains">Orleans' grain factory.</param>
    /// <returns>A task that completes when the grain has handled the message.</returns>
    public static Task Handle(UserAccessPublished message, IGrainFactory grains) =>
        grains.GetGrain<IUserAccessGrain>(message.UserId)
            .ReplaceAsync(
                message.IsDisabled,
                message.IsDeleted,
                [
                    .. message.Grants.Select(grant => new GrantState(
                        grant.Namespace,
                        Enum.Parse<NamespaceRole>(grant.Role)))
                ],
                message.Seq);
}
