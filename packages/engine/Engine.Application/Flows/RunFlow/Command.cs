using System.Text.Json;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Domain.Authorization;
using Engine.Application.Authorization;

namespace Engine.Application.Flows.RunFlow;

/// <summary>
/// Triggers a Flow by hand with a given payload, as its Execution Mode allows. The caller must hold the Engineer role.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Namespace">The Namespace of the Flow.</param>
/// <param name="Flow">The name of the Flow.</param>
/// <param name="Trigger">The trigger Node to start from; none when the Flow has exactly one.</param>
/// <param name="Payload">What the trigger carries, kept in the <c>trigger</c> variable.</param>
public sealed record Command(Guid CallerId, string Namespace, string Flow, string? Trigger, JsonElement Payload)
    : IRequest<Result<FlowRun>>;

internal sealed class Handler(IRoleGrants grants, IFlowRuntime flows) : IRequestHandler<Command, Result<FlowRun>>
{
    public async Task<Result<FlowRun>> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var access = await grants.RequireRoleAsync(
            request.CallerId,
            request.Namespace,
            NamespaceRole.Engineer,
            cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        return await flows.RunAsync(
            request.Namespace,
            request.Flow,
            request.Trigger,
            request.Payload,
            cancellationToken);
    }
}
