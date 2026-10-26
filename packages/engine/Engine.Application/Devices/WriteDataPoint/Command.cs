using System.Text.Json;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Core.Domain.Authorization;
using Engine.Application.Authorization;

namespace Engine.Application.Devices.WriteDataPoint;

/// <summary>
/// Writes a writable Data Point of a Device and waits for the device to acknowledge.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Namespace">The Namespace of the Device.</param>
/// <param name="Device">The name of the Device.</param>
/// <param name="DataPoint">The name of the Data Point.</param>
/// <param name="Value">The value to write.</param>
public sealed record Command(Guid CallerId, string Namespace, string Device, string DataPoint, JsonElement Value)
    : IRequest<Result>;

internal sealed class Handler(IRoleGrants grants, IDeviceRuntime devices) : IRequestHandler<Command, Result>
{
    public async Task<Result> HandleAsync(Command request, CancellationToken cancellationToken = default)
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

        return await devices.WriteDataPointAsync(
            request.Namespace,
            request.Device,
            request.DataPoint,
            request.Value,
            cancellationToken);
    }
}
