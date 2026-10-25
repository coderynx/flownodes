using Core.Contracts.Resources;
using Engine.Infrastructure.Namespaces;
using Wolverine.Attributes;

namespace Engine.Infrastructure.Messaging;

// HACK: public and documented only because Wolverine discovers handlers among public types; nothing calls it.
/// <summary>
/// Hands the Registry's Resource messages to the Namespace's grain (ADR-0014).
/// </summary>
[StickyHandler("engine-resources")]
public static class NamespaceMessageHandler
{
    private const string NamespaceKind = "Namespace";
    private const string ConnectionKind = "Connection";
    private const string DeviceKind = "Device";

    /// <summary>
    /// Handles a Resource applied. A Namespace is applied to its grain; a Connection or Device goes through
    /// its Namespace grain to its own grain; any other Resource only advances the Namespace grain's sequence number.
    /// </summary>
    /// <param name="message">The Registry's message.</param>
    /// <param name="grains">Orleans' grain factory.</param>
    /// <returns>A task that completes when the grain has handled the message.</returns>
    public static Task Handle(ResourceApplied message, IGrainFactory grains) =>
        message.Kind switch
        {
            NamespaceKind => grains.GetGrain<INamespaceGrain>(message.Name).ApplyAsync(message.Uid, message.Seq),
            ConnectionKind or DeviceKind => grains.GetGrain<INamespaceGrain>(message.Namespace)
                .ResourceAppliedAsync(
                    message.Kind,
                    message.Name,
                    message.Uid,
                    message.Generation,
                    message.Seq,
                    message.Spec.GetRawText()),
            _ => grains.GetGrain<INamespaceGrain>(message.Namespace).ObserveAsync(message.Seq)
        };

    /// <summary>
    /// Handles a Resource deleted. A Namespace is deleted from its grain; any other Resource only advances its
    /// Namespace grain's sequence number.
    /// </summary>
    /// <param name="message">The Registry's message.</param>
    /// <param name="grains">Orleans' grain factory.</param>
    /// <returns>A task that completes when the grain has handled the message.</returns>
    public static Task Handle(ResourceDeleted message, IGrainFactory grains) =>
        message.Kind switch
        {
            NamespaceKind => grains.GetGrain<INamespaceGrain>(message.Name).DeleteAsync(message.Uid, message.Seq),
            ConnectionKind or DeviceKind => grains.GetGrain<INamespaceGrain>(message.Namespace)
                .ResourceDeletedAsync(message.Kind, message.Name, message.Uid, message.Seq),
            _ => grains.GetGrain<INamespaceGrain>(message.Namespace).ObserveAsync(message.Seq)
        };
}
