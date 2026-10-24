using Coderynx.MediatorKit.Abstractions;
using Core.Contracts.Resources;
using Identity.Domain.Namespaces;

namespace Identity.Infrastructure.Messaging;

/// <summary>
/// Consumes the Registry's Resource messages about Namespaces, so Identity knows which Namespaces a role can be
/// granted in (ADR-0026). It is public because Wolverine discovers handlers only among public types.
/// </summary>
public static class ResourceMessageHandler
{
    /// <summary>
    /// Records a Namespace the Registry created; a message about any other Kind is ignored.
    /// </summary>
    /// <param name="message">The Registry's message.</param>
    /// <param name="sender">The mediator Identity's use cases run through.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    /// <returns>A task that completes when the message is handled.</returns>
    public static async Task Handle(ResourceApplied message, ISender sender, CancellationToken cancellationToken)
    {
        if (message.Kind == KnownNamespace.Kind)
        {
            await sender.SendAsync(
                new Application.Namespaces.ApplyNamespace.Command(message.Name, message.Uid, message.Seq),
                cancellationToken);
        }
    }

    /// <summary>
    /// Forgets a Namespace the Registry deleted and drops its Role Grants; a message about any other Kind is ignored.
    /// </summary>
    /// <param name="message">The Registry's message.</param>
    /// <param name="sender">The mediator Identity's use cases run through.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    /// <returns>A task that completes when the message is handled.</returns>
    public static async Task Handle(ResourceDeleted message, ISender sender, CancellationToken cancellationToken)
    {
        if (message.Kind == KnownNamespace.Kind)
        {
            await sender.SendAsync(
                new Application.Namespaces.RemoveNamespace.Command(message.Name, message.Uid, message.Seq),
                cancellationToken);
        }
    }
}
