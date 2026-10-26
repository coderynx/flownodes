using System.Text.Json;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Domain.Authorization;
using Engine.Application.Authorization;

namespace Engine.Application.Devices.InvokeAction;

/// <summary>
/// Calls an Action of a Device and returns its result.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Namespace">The Namespace of the Device.</param>
/// <param name="Device">The name of the Device.</param>
/// <param name="Action">The name of the Action.</param>
/// <param name="Parameters">The parameters as JSON.</param>
public sealed record Command(Guid CallerId, string Namespace, string Device, string Action, JsonElement Parameters)
    : IRequest<Result<JsonElement>>;

internal sealed class Handler(IRoleGrants grants, IDeviceRuntime devices)
    : IRequestHandler<Command, Result<JsonElement>>
{
    public async Task<Result<JsonElement>> HandleAsync(Command request, CancellationToken cancellationToken = default)
    {
        var access = await grants.RequireRoleAsync(
            request.CallerId,
            request.Namespace,
            NamespaceRole.Operator,
            cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        return await devices.InvokeActionAsync(
            request.Namespace,
            request.Device,
            request.Action,
            request.Parameters,
            cancellationToken);
    }
}
