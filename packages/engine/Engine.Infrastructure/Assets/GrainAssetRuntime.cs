using System.Text.Json;
using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Engine.Application.Assets;
using Engine.Application.Devices;
using Engine.Infrastructure.Connections;

namespace Engine.Infrastructure.Assets;

internal sealed class GrainAssetRuntime(IGrainFactory grains) : IAssetRuntime
{
    private static readonly string[] Qualities = ["good", "uncertain", "bad"];

    public async Task<Result<IReadOnlyList<DataPointReading>>> ListDataPointsAsync(
        string @namespace,
        string asset,
        CancellationToken cancellationToken)
    {
        var snapshots = await Grain(@namespace, asset).GetDataPointsAsync();
        if (snapshots is null)
        {
            return Error.NotFound("asset.not_found", $"Asset '{asset}' does not exist.");
        }

        return Result.Found<IReadOnlyList<DataPointReading>>(
        [
            .. snapshots.Select(snapshot => new DataPointReading(
                snapshot.Name,
                snapshot.Type,
                snapshot.Writable,
                snapshot.Unit,
                snapshot.Min,
                snapshot.Max,
                snapshot.Values,
                snapshot.Value is null ? null : JsonDocument.Parse(snapshot.Value).RootElement.Clone(),
                Qualities[snapshot.Quality],
                snapshot.Timestamp))
        ]);
    }

    public async Task<Result> WriteDataPointAsync(
        string @namespace,
        string asset,
        string dataPoint,
        JsonElement value,
        CancellationToken cancellationToken)
    {
        var result = await Grain(@namespace, asset).WriteAsync(dataPoint, value.GetRawText());

        return result.Ok ? Result.Updated() : ToError(result);
    }

    private static Error ToError(CallResult result) =>
        result.Code switch
        {
            "NotFound" => Error.NotFound("asset.not_found", result.Message!),
            "ReadOnly" or "InvalidValue" => Error.InvalidInput($"asset.{result.Code}", result.Message!),
            "ConnectionNotReady" => Error.Conflict("asset.connection_not_ready", result.Message!),
            _ => Error.Custom($"asset.{result.Code}", result.Message!)
        };

    private IAssetGrain Grain(string @namespace, string asset) =>
        grains.GetGrain<IAssetGrain>($"{@namespace}/{asset}");
}
