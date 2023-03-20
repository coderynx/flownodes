using Flownodes.Sdk;
using Flownodes.Sdk.Resourcing;
using Flownodes.Shared.Resourcing;
using Flownodes.Worker.Services;

namespace Flownodes.Worker.Resourcing;

[GrainType(FlownodesEntityNames.Device)]
internal sealed class DeviceGrain : ResourceGrain, IDeviceGrain
{
    private readonly ILogger<DeviceGrain> _logger;

    public DeviceGrain(IPluginProvider pluginProvider, ILogger<DeviceGrain> logger,
        IEnvironmentService environmentService) :
        base(logger, environmentService, pluginProvider)
    {
        _logger = logger;
    }

    private async Task ExecuteTimerBehaviourAsync(object arg)
    {
        var context = GetResourceContext();

        if (Behaviour is null) throw new InvalidOperationException("Behaviour cannot be null");

        var deviceBehaviour = (IReadableDeviceBehaviour)Behaviour;
        await deviceBehaviour.OnPullStateAsync(context);

        var @event = new UpdateResourceStateEvent(context.State);
        await RaiseConditionalEvent(@event);
    }

    protected override Task OnBehaviourChangedAsync()
    {
        if (Behaviour is null) throw new InvalidOperationException("Behaviour cannot be null");

        var isReadable = Behaviour
            .GetType()
            .IsAssignableTo(typeof(IReadableDeviceBehaviour));

        if (!isReadable)
            return Task.CompletedTask;

        if (State.Configuration?.GetValueOrDefault("updateStateTimeSpan") is not int updateState)
            return Task.CompletedTask;

        var timeSpan = TimeSpan.FromSeconds(updateState);
        RegisterTimer(ExecuteTimerBehaviourAsync, null, timeSpan, timeSpan);
        return Task.CompletedTask;
    }

    protected override async Task OnUpdateStateAsync(Dictionary<string, object?> newState)
    {
        if (Behaviour is null) throw new InvalidOperationException("Behavior cannot be null");

        var deviceBehaviour = (IWritableDeviceBehaviour)Behaviour;
        await deviceBehaviour.OnPushStateAsync(newState, GetResourceContext());

        _logger.LogInformation("Applied new state for device {@DeviceId}", Id);
    }
}