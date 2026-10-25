using System.Text.Json;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;

namespace Engine.Application.Devices.WriteDataPoint;

/// <summary>
/// Writes a writable Data Point of a Device and waits for the device to acknowledge.
/// </summary>
/// <param name="Namespace">The Namespace of the Device.</param>
/// <param name="Device">The name of the Device.</param>
/// <param name="DataPoint">The name of the Data Point.</param>
/// <param name="Value">The value to write.</param>
public sealed record Command(string Namespace, string Device, string DataPoint, JsonElement Value) : IRequest<Result>;

internal sealed class Handler(IDeviceRuntime devices) : IRequestHandler<Command, Result>
{
    public Task<Result> HandleAsync(Command request, CancellationToken cancellationToken = default) =>
        devices.WriteDataPointAsync(
            request.Namespace,
            request.Device,
            request.DataPoint,
            request.Value,
            cancellationToken);
}
