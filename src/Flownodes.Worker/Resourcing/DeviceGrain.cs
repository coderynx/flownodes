using Flownodes.Sdk.Entities;
using Flownodes.Sdk.Resourcing.Behaviours;
using Flownodes.Shared.Eventing;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Worker.Extendability;
using Flownodes.Worker.Extensions;
using Flownodes.Worker.Resourcing.Persistence;
using Flownodes.Worker.Services;

namespace Flownodes.Worker.Resourcing;

[GrainType(FlownodesEntityNames.Device)]
internal sealed class DeviceGrain : ResourceGrain, IDeviceGrain
{
    private readonly ILogger<DeviceGrain> _logger;

    public DeviceGrain(IComponentProvider componentProvider, ILogger<DeviceGrain> logger,
        IEnvironmentService environmentService) :
        base(logger, environmentService, componentProvider)
    {
        _logger = logger;
    }

    private async Task ExecuteTimerBehaviourAsync(object arg)
    {
        var context = GetResourceContext();

        var deviceBehaviour = (IReadableDeviceBehaviour)Behaviour!;
        await deviceBehaviour.OnPullStateAsync(context);

        if (State.State!.HasChanged(context.State))
        {
            var @event = new UpdateResourceStateEvent(context.State);
            await RaiseConditionalEvent(@event);
            await EventBook.RegisterEventAsync(EventKind.UpdateResourceState, Id);   
        }
    }

    protected override Task OnBehaviourChangedAsync()
    {
        var isReadable = Behaviour!
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