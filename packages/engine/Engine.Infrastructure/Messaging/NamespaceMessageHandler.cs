using Core.Contracts.Resources;
using Engine.Infrastructure.Namespaces;

namespace Engine.Infrastructure.Messaging;

// HACK: public and documented only because Wolverine discovers handlers among public types; nothing calls it.
/// <summary>
/// Hands the Registry's Namespace messages to the Namespace's grain (ADR-0014).
/// </summary>
public static class NamespaceMessageHandler
{
    private const string NamespaceKind = "Namespace";

    /// <summary>
    /// Handles a Resource applied; only a Namespace reaches a Namespace grain so far.
    /// </summary>
    /// <param name="message">The Registry's message.</param>
    /// <param name="grains">Orleans' grain factory.</param>
    /// <returns>A task that completes when the grain has handled the message.</returns>
    public static Task Handle(ResourceApplied message, IGrainFactory grains) =>
        message.Kind == NamespaceKind
            ? grains.GetGrain<INamespaceGrain>(message.Name).ApplyAsync(message.Uid, message.Seq)
            : Task.CompletedTask;

    /// <summary>
    /// Handles a Resource deleted; only a Namespace reaches a Namespace grain so far.
    /// </summary>
    /// <param name="message">The Registry's message.</param>
    /// <param name="grains">Orleans' grain factory.</param>
    /// <returns>A task that completes when the grain has handled the message.</returns>
    public static Task Handle(ResourceDeleted message, IGrainFactory grains) =>
        message.Kind == NamespaceKind
            ? grains.GetGrain<INamespaceGrain>(message.Name).DeleteAsync(message.Uid, message.Seq)
            : Task.CompletedTask;
}
