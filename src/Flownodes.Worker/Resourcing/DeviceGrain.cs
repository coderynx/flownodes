using Flownodes.Sdk.Entities;
using Flownodes.Sdk.Resourcing.Behaviours;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Worker.Extendability;
using Flownodes.Worker.Extensions;
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

        if (!State.Metadata.ContainsAll(bag.Metadata)) await WriteMetadataAsync(bag.Metadata);
        if (!State.Configuration!.ContainsAll(bag.Configuration)) await UpdateConfigurationAsync(bag.Configuration);
        if (!State.State!.ContainsAll(bag.State)) await WriteStateAsync(bag.State);
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

    protected override async Task OnWriteStateAsync(Dictionary<string, object?> state)
    {
        if (Behaviour is null) throw new InvalidOperationException("Behavior cannot be null");

        var deviceBehaviour = (IWritableDeviceBehaviour)Behaviour;
        await deviceBehaviour.OnPushStateAsync(state);

        _logger.LogInformation("Applied new state for device {@DeviceId}", Id);
    }
}