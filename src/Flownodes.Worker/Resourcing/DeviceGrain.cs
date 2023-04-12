using System.Collections.Immutable;
using Flownodes.Sdk.Entities;
using Flownodes.Sdk.Resourcing;
using Flownodes.Sdk.Resourcing.Behaviours;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Worker.Extendability;
using Flownodes.Worker.Resourcing.Persistence;
using Orleans.Runtime;

namespace Flownodes.Worker.Resourcing;

[GrainType(FlownodesEntityNames.Device)]
internal sealed class DeviceGrain : ResourceGrain, IDeviceGrain
{
    private readonly IExtensionProvider _extensionProvider;
    private readonly ILogger<DeviceGrain> _logger;
    private IDeviceBehaviour? _behaviour;

    private readonly IPersistentState<BehaviourId> _behaviourId;

    public DeviceGrain(IExtensionProvider extensionProvider, ILogger<DeviceGrain> logger,
        IPersistentStateFactory stateFactory, IGrainContext grainContext) :
        base(logger, stateFactory, grainContext)
    {
        _behaviourId =
            stateFactory.Create<BehaviourId>(grainContext, new PersistentStateAttribute("resourceBehaviourIdStore"));
        _extensionProvider = extensionProvider;
        _logger = logger;
    }

    public async Task UpdateBehaviourId(string behaviourId)
    {
        _behaviourId.State.Value = behaviourId;
        await _behaviourId.WriteStateAsync();
        await OnUpdateBehaviourAsync();

        _logger.LogInformation("Updated BehaviourId of ResourceGrain {@ResourceId}", Id);
    }

    public async ValueTask<BaseResourceSummary> GetSummary()
    {
        return new DeviceSummary(Id, Metadata, _behaviourId.State.Value, await GetConfiguration(), await GetState());
    }

    private async Task ExecuteTimerBehaviourAsync(object arg)
    {
        var deviceBehaviour = (IReadableDeviceBehaviour)_behaviour!;
        var bag = await deviceBehaviour.OnPullStateAsync();

        await WriteMetadataConditionalAsync(bag.Metadata);
        await WriteConfigurationConditionalAsync(bag.Configuration);
        await WriteStateConditionalAsync(bag.State);
    }

    private async Task OnUpdateBehaviourAsync()
    {
        if (_behaviourId.State.Value is null) return;

        var configuration = await GetConfiguration();
        var state = await GetState();
        var context = new DeviceContext(Id, Metadata.ToImmutableDictionary(), configuration.ToImmutableDictionary(),
            state.ToImmutableDictionary());

        _behaviour =
            _extensionProvider.ResolveBehaviour<IDeviceBehaviour, DeviceContext>(_behaviourId.State.Value, context);
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