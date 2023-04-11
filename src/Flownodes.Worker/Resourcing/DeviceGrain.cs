using System.Collections.Immutable;
using Flownodes.Sdk.Entities;
using Flownodes.Sdk.Resourcing;
using Flownodes.Sdk.Resourcing.Behaviours;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Worker.Extendability;
using Orleans.Runtime;

namespace Flownodes.Worker.Resourcing;

[GrainType(FlownodesEntityNames.Device)]
internal sealed class DeviceGrain : ResourceGrain, IDeviceGrain
{
    private readonly IExtensionProvider _extensionProvider;
    private readonly ILogger<DeviceGrain> _logger;
    private IDeviceBehaviour? _behaviour;

    public DeviceGrain(IExtensionProvider extensionProvider, ILogger<DeviceGrain> logger,
        IPersistentStateFactory stateFactory, IGrainContext grainContext) :
        base(logger, stateFactory, grainContext)
    {
        _extensionProvider = extensionProvider;
        _logger = logger;
    }

    public async ValueTask<BaseResourceSummary> GetSummary()
    {
        return new DeviceSummary(Id, Metadata, BehaviourId, await GetConfiguration(), await GetState());
    }

    private async Task ExecuteTimerBehaviourAsync(object arg)
    {
        var deviceBehaviour = (IReadableDeviceBehaviour)_behaviour!;
        var bag = await deviceBehaviour.OnPullStateAsync();

        await WriteMetadataConditionalAsync(bag.Metadata);
        await WriteConfigurationConditionalAsync(bag.Configuration);
        await WriteStateConditionalAsync(bag.State);
    }

    protected override async Task OnUpdateBehaviourAsync()
    {
        if (BehaviourId is null) return;

        var configuration = await GetConfiguration();
        var state = await GetState();
        var context = new DeviceContext(Id, Metadata.ToImmutableDictionary(), configuration.ToImmutableDictionary(),
            state.ToImmutableDictionary());

        _behaviour = _extensionProvider.ResolveBehaviour<IDeviceBehaviour, DeviceContext>(BehaviourId, context);
        await _behaviour.OnSetupAsync();

        var isReadable = _behaviour!
            .GetType()
            .IsAssignableTo(typeof(IReadableDeviceBehaviour));

        if (!isReadable) return;

        if (configuration.GetValueOrDefault("updateStateTimeSpan") is not int updateState) return;

        var timeSpan = TimeSpan.FromSeconds(updateState);
        RegisterTimer(ExecuteTimerBehaviourAsync, null, timeSpan, timeSpan);
    }

    protected override async Task OnUpdateStateAsync(Dictionary<string, object?> state)
    {
        if (_behaviour is null) throw new InvalidOperationException("Behavior cannot be null");

        var deviceBehaviour = (IWritableDeviceBehaviour)_behaviour;
        await deviceBehaviour.OnPushStateAsync(state);

        _logger.LogInformation("Applied new state for device {@DeviceId}", Id);
    }
}