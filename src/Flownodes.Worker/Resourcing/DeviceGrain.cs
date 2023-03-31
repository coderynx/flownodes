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

    public DeviceGrain(IExtensionProvider extensionProvider, ILogger<DeviceGrain> logger,
        IEnvironmentService environmentService) :
        base(logger, environmentService, extensionProvider)
    {
        _logger = logger;
    }

    private async Task ExecuteTimerBehaviourAsync(object arg)
    {
        var deviceBehaviour = (IReadableDeviceBehaviour)Behaviour!;
        var bag = await deviceBehaviour.OnPullStateAsync();

        if (!State.Metadata.ContainsAll(bag.Metadata))
        {
            var @event = new UpdateResourceMetadataEvent(bag.Metadata);
            await RaiseConditionalEvent(@event);
            await EventBook.RegisterEventAsync(EventKind.UpdateResourceMetadata, Id);
        }

        if (!State.Configuration!.ContainsAll(bag.Configuration))
        {
            var @event = new UpdateResourceConfigurationEvent(bag.Configuration);
            await RaiseConditionalEvent(@event);
            await EventBook.RegisterEventAsync(EventKind.UpdateResourceConfiguration, Id);
        }

        if (!State.State!.ContainsAll(bag.State))
        {
            var @event = new UpdateResourceStateEvent(bag.State);
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
        await deviceBehaviour.OnPushStateAsync(newState);

        _logger.LogInformation("Applied new state for device {@DeviceId}", Id);
    }
}