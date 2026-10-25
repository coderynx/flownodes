using System.Text.Json;
using Engine.Infrastructure.Integrations;
using Flownodes.Sdk;

namespace Engine.Infrastructure.Connections;

/// <summary>
/// Runs one Connection of an Integration off the Orleans scheduler: connects, keeps the declared Devices told to it,
/// and starts over with capped backoff whenever it fails (ADR-0003).
/// </summary>
internal sealed class ConnectionRuntime(
    LoadedIntegration integration,
    JsonElement config,
    IConnectionEvents events,
    ConnectionGrainOptions options) : IAsyncDisposable
{
    private readonly CancellationTokenSource _stopping = new();
    private readonly SemaphoreSlim _deviceSync = new(1, 1);
    private readonly JsonElement _config = config.Clone();
    private IReadOnlyList<DeviceBinding> _devices = [];
    private volatile Attempt? _current;
    private Task _loop = Task.CompletedTask;
    public bool IsConnected => _current is { Started: true };
    public void Start() => _loop = Task.Run(RunAsync, CancellationToken.None);

    public async Task SetDevicesAsync(IReadOnlyList<DeviceBinding> devices)
    {
        _devices = devices;
        await PushDevicesAsync(_current);
    }

    public Task WriteAsync(string device, string dataPoint, JsonElement value, CancellationToken cancellationToken) =>
        Task.Run(() => Connected().WriteAsync(device, dataPoint, value, cancellationToken), cancellationToken);

    public Task<JsonElement> InvokeAsync(
        string device,
        string action,
        JsonElement parameters,
        string invocationId,
        CancellationToken cancellationToken) =>
        Task.Run(
            () => Connected().InvokeAsync(device, action, parameters, invocationId, cancellationToken),
            cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await _stopping.CancelAsync();
        await _loop.ConfigureAwait(false);
        _stopping.Dispose();
    }

    private IConnection Connected() =>
        _current is { Started: true } attempt
            ? attempt.Connection!
            : throw new InvalidOperationException("The Connection is not connected.");

    private async Task RunAsync()
    {
        var backoff = options.InitialBackoff;
        while (!_stopping.IsCancellationRequested)
        {
            var attempt = new Attempt(events);
            try
            {
                events.Connecting();
                attempt.Connection = integration.Integration.CreateConnection(integration.Services, _config, attempt);
                await attempt.Connection.StartAsync(_stopping.Token);
                attempt.Started = true;
                _current = attempt;
                await PushDevicesAsync(attempt);
                backoff = options.InitialBackoff;
                var message = await attempt.Failure.Task.WaitAsync(_stopping.Token);
                events.Failed(message);
            }
            catch (OperationCanceledException) when (_stopping.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                events.Failed(exception.Message);
            }
            finally
            {
                _current = null;
                attempt.Ended = true;
                await DisposeAttemptAsync(attempt);
            }

            try
            {
                await Task.Delay(backoff, _stopping.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            backoff = TimeSpan.FromTicks(Math.Min(backoff.Ticks * 2, options.MaxBackoff.Ticks));
        }
    }

    private async Task PushDevicesAsync(Attempt? attempt)
    {
        if (attempt is not { Started: true })
        {
            return;
        }

        await _deviceSync.WaitAsync(_stopping.Token);
        try
        {
            await attempt.Connection!.SetDevicesAsync(_devices, _stopping.Token);
        }
        finally
        {
            _deviceSync.Release();
        }
    }

    private static async Task DisposeAttemptAsync(Attempt attempt)
    {
        if (attempt.Connection is null)
        {
            return;
        }

        try
        {
            await attempt.Connection.DisposeAsync();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            _ = exception;
        }
    }

    private sealed class Attempt(IConnectionEvents events) : IConnectionSink
    {
        public IConnection? Connection { get; set; }
        public bool Started { get; set; }
        public bool Ended { get; set; }
        public TaskCompletionSource<string> Failure { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void ReportConnectivity(Connectivity connectivity, string? message = null)
        {
            if (!Ended)
            {
                events.Connectivity(connectivity, message);
            }
        }

        public void ReportFailure(string message) => Failure.TrySetResult(message);

        public void ReportValue(
            string device,
            string dataPoint,
            JsonElement value,
            DateTimeOffset sourceTimestamp,
            Quality quality = Quality.Good)
        {
            if (!Ended)
            {
                events.Value(device, dataPoint, value.Clone(), sourceTimestamp, quality);
            }
        }

        public void ReportDeviceState(string device, DeviceState state)
        {
            if (!Ended)
            {
                events.DeviceState(device, state);
            }
        }

        public void ReportDiscoveries(IReadOnlyList<Discovery> discoveries)
        {
            if (!Ended)
            {
                events.Discoveries(discoveries);
            }
        }
    }
}
