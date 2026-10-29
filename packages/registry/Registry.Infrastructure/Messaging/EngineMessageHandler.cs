using Coderynx.MediatorKit.Abstractions;
using Core.Contracts.Resources;
using ListNamespaceNames = Registry.Application.Namespaces.ListNamespaceNames;
using ReportStatus = Registry.Application.Resources.ReportStatus;
using ResyncNamespace = Registry.Application.Namespaces.ResyncNamespace;

namespace Registry.Infrastructure.Messaging;

// HACK: public and documented only because Wolverine discovers handlers among public types; nothing calls it.
/// <summary>
/// Hands the Engine's requests and Status reports to the use cases that handle them (ADR-0014).
/// </summary>
public static class EngineMessageHandler
{
    /// <summary>
    /// Handles a request for the name of every Namespace.
    /// </summary>
    /// <param name="_">The Engine's message, which carries nothing.</param>
    /// <param name="sender">The Registry's mediator.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    /// <returns>The answer, which Wolverine publishes through the Registry's outbox.</returns>
    public static async Task<NamespacesListed> Handle(
        NamespacesListRequested _,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var names = await sender.SendAsync(new ListNamespaceNames.Query(), cancellationToken);

        return new NamespacesListed(names.Value);
    }

    /// <summary>
    /// Handles a request to publish a Namespace again.
    /// </summary>
    /// <param name="message">The Engine's message.</param>
    /// <param name="sender">The Registry's mediator.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    /// <returns>A task that completes when the message is handled.</returns>
    public static Task Handle(NamespaceResyncRequested message, ISender sender, CancellationToken cancellationToken) =>
        sender.SendAsync(new ResyncNamespace.Command(message.Namespace), cancellationToken);

    /// <summary>
    /// Handles a Status the Engine reported.
    /// </summary>
    /// <param name="message">The Engine's message.</param>
    /// <param name="sender">The Registry's mediator.</param>
    /// <param name="cancellationToken">Cancels the handling.</param>
    /// <returns>A task that completes when the message is handled.</returns>
    public static Task Handle(StatusReported message, ISender sender, CancellationToken cancellationToken) =>
        sender.SendAsync(
            new ReportStatus.Command(
                message.Namespace,
                message.Kind,
                message.Name,
                message.Uid,
                message.ObservedGeneration,
                message.StatusSeq,
                message.Status),
            cancellationToken);
}
