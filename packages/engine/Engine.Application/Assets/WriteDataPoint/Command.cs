using System.Text.Json;
using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;

namespace Engine.Application.Assets.WriteDataPoint;

/// <summary>
/// Writes a writable Data Point of an Asset, writing through to the Device when it is mapped to one.
/// </summary>
/// <param name="Namespace">The Namespace of the Asset.</param>
/// <param name="Asset">The name of the Asset.</param>
/// <param name="DataPoint">The name of the Data Point.</param>
/// <param name="Value">The value to write.</param>
public sealed record Command(string Namespace, string Asset, string DataPoint, JsonElement Value) : IRequest<Result>;

internal sealed class Handler(IAssetRuntime assets) : IRequestHandler<Command, Result>
{
    public Task<Result> HandleAsync(Command request, CancellationToken cancellationToken = default) =>
        assets.WriteDataPointAsync(
            request.Namespace,
            request.Asset,
            request.DataPoint,
            request.Value,
            cancellationToken);
}
