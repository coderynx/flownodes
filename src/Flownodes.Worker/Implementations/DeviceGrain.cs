using Flownodes.Sdk.Resourcing;
using Flownodes.Shared.Interfaces;
using Flownodes.Worker.Services;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

[GrainType("device")]
internal sealed class DeviceGrain : ResourceGrain, IDeviceGrain
{
    public DeviceGrain(IPluginProvider pluginProvider, ILogger<DeviceGrain> logger,
        IEnvironmentService environmentService, IPersistentStateFactory persistentStateFactory,
        IGrainContext grainContext) :
        base(logger, environmentService, pluginProvider, persistentStateFactory, grainContext)
    {
    }

    private async Task ExecuteTimerBehaviourAsync(object arg)
    {
        var context = GetResourceContext();

        if (Behaviour is null) throw new InvalidOperationException("Behaviour cannot be null");

        var deviceBehaviour = (IReadableDeviceBehaviour)Behaviour;
        await deviceBehaviour.OnPullStateAsync(context);

        State.UpdateState(context.State); // TODO: Evaluate if there's a better way.

        Logger.LogDebug("Updated state for device {@DeviceId}: {@State}", Id, State.Properties);
    }

    protected override Task OnBehaviourChangedAsync()
    {
        if (Behaviour is null) throw new InvalidOperationException("Behaviour cannot be null");

        var isReadable = Behaviour
            .GetType()
            .IsAssignableTo(typeof(IReadableDeviceBehaviour));

        var isWritable = Behaviour
            .GetType()
            .IsAssignableTo(typeof(IWritableDeviceBehaviour));

        if (!isReadable)
            return Task.CompletedTask;

        if (Configuration.Properties.GetValueOrDefault("updateStateTimeSpan") is not int updateState)
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

        Logger.LogInformation("Applied new state for device {DeviceId}", Id);
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Activated device {DeviceId}", Id);
        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Deactivated device {DeviceId}", Id);
        return base.OnDeactivateAsync(reason, cancellationToken);
    }
}