using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;

namespace Engine.Application.Devices.ListDataPoints;

/// <summary>
/// Lists a Device's Data Points with their latest value, Quality and source timestamp.
/// </summary>
/// <param name="Namespace">The Namespace of the Device.</param>
/// <param name="Device">The name of the Device.</param>
public sealed record Query(string Namespace, string Device) : IRequest<Result<IReadOnlyList<DataPointReading>>>;

internal sealed class Handler(IDeviceRuntime devices) : IRequestHandler<Query, Result<IReadOnlyList<DataPointReading>>>
{
    public Task<Result<IReadOnlyList<DataPointReading>>> HandleAsync(
        Query request,
        CancellationToken cancellationToken = default) =>
        devices.ListDataPointsAsync(request.Namespace, request.Device, cancellationToken);
}
