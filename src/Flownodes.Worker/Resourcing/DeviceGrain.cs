using Flownodes.Sdk.Entities;
using Flownodes.Sdk.Resourcing.Behaviours;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Worker.Extendability;
using Flownodes.Worker.Services;
using Orleans.Runtime;

namespace Flownodes.Worker.Resourcing;

[GrainType(FlownodesEntityNames.Device)]
internal sealed class DeviceGrain : ResourceGrain, IDeviceGrain
{
    private readonly ILogger<DeviceGrain> _logger;

    public DeviceGrain(IExtensionProvider extensionProvider, ILogger<DeviceGrain> logger,
        IEnvironmentService environmentService, IPersistentStateFactory stateFactory, IGrainContext grainContext) :
        base(logger, environmentService, extensionProvider, stateFactory, grainContext)
    {
        _logger = logger;
    }

    public async ValueTask<BaseResourceSummary> GetSummary()
    {
        return new DeviceSummary(Id, Metadata, BehaviourId, await GetConfiguration(), await GetState());
    }

    private async Task ExecuteTimerBehaviourAsync(object arg)
    {
        var deviceBehaviour = (IReadableDeviceBehaviour)Behaviour!;
        var bag = await deviceBehaviour.OnPullStateAsync();

        await WriteMetadataConditionalAsync(bag.Metadata);
        await WriteConfigurationConditionalAsync(bag.Configuration);
        await WriteStateConditionalAsync(bag.State);
    }

    protected override async Task OnBehaviourChangeAsync()
    {
        var isReadable = Behaviour!
            .GetType()
            .IsAssignableTo(typeof(IReadableDeviceBehaviour));

        if (!isReadable) return;

        var configuration = await GetConfiguration();
        if (configuration.GetValueOrDefault("updateStateTimeSpan") is not int updateState) return;

        var timeSpan = TimeSpan.FromSeconds(updateState);
        RegisterTimer(ExecuteTimerBehaviourAsync, null, timeSpan, timeSpan);
    }

    protected override async Task OnUpdateStateAsync(Dictionary<string, object?> state)
    {
        if (Behaviour is null) throw new InvalidOperationException("Behavior cannot be null");

        var deviceBehaviour = (IWritableDeviceBehaviour)Behaviour;
        await deviceBehaviour.OnPushStateAsync(state);

        _logger.LogInformation("Applied new state for device {@DeviceId}", Id);
    }
}