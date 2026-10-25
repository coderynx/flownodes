using System.Text.Json;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;

namespace Engine.Application.Devices.InvokeAction;

/// <summary>
/// Calls an Action of a Device and returns its result.
/// </summary>
/// <param name="Namespace">The Namespace of the Device.</param>
/// <param name="Device">The name of the Device.</param>
/// <param name="Action">The name of the Action.</param>
/// <param name="Parameters">The parameters as JSON.</param>
public sealed record Command(string Namespace, string Device, string Action, JsonElement Parameters)
    : IRequest<Result<JsonElement>>;

internal sealed class Handler(IDeviceRuntime devices) : IRequestHandler<Command, Result<JsonElement>>
{
    public Task<Result<JsonElement>> HandleAsync(Command request, CancellationToken cancellationToken = default) =>
        devices.InvokeActionAsync(
            request.Namespace,
            request.Device,
            request.Action,
            request.Parameters,
            cancellationToken);
}
