using System.Text.Json;
using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Engine.Application.Devices;
using Engine.Infrastructure.Connections;

namespace Engine.Infrastructure.Devices;

internal sealed class GrainDeviceRuntime(IGrainFactory grains) : IDeviceRuntime
{
    private static readonly string[] Qualities = ["good", "uncertain", "bad"];

    public async Task<Result<IReadOnlyList<DataPointReading>>> ListDataPointsAsync(
        string @namespace,
        string device,
        CancellationToken cancellationToken)
    {
        var snapshots = await Grain(@namespace, device).GetDataPointsAsync();
        if (snapshots is null)
        {
            return NotFound(device);
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
        string device,
        string dataPoint,
        JsonElement value,
        CancellationToken cancellationToken)
    {
        var result = await Grain(@namespace, device).WriteAsync(dataPoint, value.GetRawText());

        return result.Ok ? Result.Updated() : ToError(result);
    }

    public async Task<Result<JsonElement>> InvokeActionAsync(
        string @namespace,
        string device,
        string action,
        JsonElement parameters,
        CancellationToken cancellationToken)
    {
        var result = await Grain(@namespace, device)
            .InvokeAsync(action, parameters.GetRawText(), Guid.NewGuid().ToString("N"));
        if (!result.Ok)
        {
            return ToError(result);
        }

        return Result.Found(JsonDocument.Parse(result.Json ?? "null").RootElement.Clone());
    }

    private static Error NotFound(string device) =>
        Error.NotFound("device.not_found", $"Device '{device}' does not exist.");

    private static Error ToError(CallResult result) =>
        result.Code switch
        {
            "NotFound" => Error.NotFound("device.not_found", result.Message!),
            "ReadOnly" or "InvalidValue" => Error.InvalidInput($"device.{result.Code}", result.Message!),
            "ConnectionNotReady" => Error.Conflict("device.connection_not_ready", result.Message!),
            _ => Error.Custom($"device.{result.Code}", result.Message!)
        };

    private IDeviceGrain Grain(string @namespace, string device) =>
        grains.GetGrain<IDeviceGrain>($"{@namespace}/{device}");
}
