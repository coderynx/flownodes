using Coderynx.MediatorKit.Abstractions;
using Core.Contracts.Resources;
using Identity.Domain.Namespaces;
using Wolverine.Attributes;

namespace Identity.Infrastructure.Messaging;

// HACK: public and documented only because Wolverine discovers handlers among public types; nothing calls it.
/// <summary>
/// Hands the Registry's Namespace messages to the use cases that record them (ADR-0026).
/// </summary>
[StickyHandler("identity-resources")]
public static class ResourceMessageHandler
{
    /// <summary>
    /// Handles a Resource applied; only a Namespace is recorded.
    /// </summary>
    /// <param name="message">The Registry's message.</param>
    /// <param name="sender">Identity's mediator.</param>
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
    /// Handles a Resource deleted; only a Namespace is forgotten.
    /// </summary>
    /// <param name="message">The Registry's message.</param>
    /// <param name="sender">Identity's mediator.</param>
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
