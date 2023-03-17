using Flownodes.Sdk;
using Flownodes.Sdk.Resourcing;
using Flownodes.Shared.Interfaces;
using Flownodes.Worker.Models;
using Flownodes.Worker.Services;

namespace Flownodes.Worker.Implementations;

[GrainType(FlownodesObjectNames.Device)]
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

        _logger.LogInformation("Applied new state for device {DeviceId}", Id);
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activated device {DeviceId}", Id);
        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivated device {DeviceId}", Id);
        return base.OnDeactivateAsync(reason, cancellationToken);
    }
}