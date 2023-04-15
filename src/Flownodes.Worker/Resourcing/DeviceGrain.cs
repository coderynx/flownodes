using System.Collections.Immutable;
using Flownodes.Sdk.Entities;
using Flownodes.Sdk.Resourcing;
using Flownodes.Sdk.Resourcing.Behaviours;
using Flownodes.Shared.Eventing;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Worker.Extendability;
using Flownodes.Worker.Extensions;
using Flownodes.Worker.Resourcing.Persistence;
using Orleans.Runtime;

namespace Flownodes.Worker.Resourcing;

[GrainType(FlownodesEntityNames.Device)]
internal sealed class DeviceGrain : ResourceGrain, IDeviceGrain
{
    private readonly IPersistentState<BehaviourId> _behaviourId;
    private readonly IJournaledStoreGrain<Dictionary<string, object?>> _configuration;
    private readonly IExtensionProvider _extensionProvider;
    private readonly ILogger<DeviceGrain> _logger;
    private readonly IJournaledStoreGrain<Dictionary<string, object?>> _state;
    private IDeviceBehaviour? _behaviour;
    private IDisposable? _timer;

    public DeviceGrain(IExtensionProvider extensionProvider, ILogger<DeviceGrain> logger,
        [PersistentState("deviceMetadata")] IPersistentState<Dictionary<string, object?>> metadata,
        [PersistentState("deviceBehaviourId")] IPersistentState<BehaviourId> behaviourId)
        : base(logger, metadata)
    {
        _behaviourId = behaviourId;
        _configuration =
            GrainFactory.GetGrain<IJournaledStoreGrain<Dictionary<string, object?>>>($"{Id}_configuration");
        _state = GrainFactory.GetGrain<IJournaledStoreGrain<Dictionary<string, object?>>>($"{Id}_state");

        _extensionProvider = extensionProvider;
        _logger = logger;
    }

    public async Task UpdateBehaviourId(string behaviourId)
    {
        _behaviourId.State.Value = behaviourId;
        await _behaviourId.WriteStateAsync();
        await OnUpdateBehaviourAsync();
        await EventBook.RegisterEventAsync(EventKind.UpdatedResource, Id);

        _logger.LogInformation("Updated BehaviourId of ResourceGrain {@ResourceId}", Id);
    }

    public async ValueTask<ResourceSummary> GetSummary()
    {
        var properties = new Dictionary<string, object?>
        {
            { "configuration", await _configuration.Get() },
            { "state", await _state.Get() }
        };

        return new ResourceSummary(Id, Metadata.State, properties);
    }

    public async Task UpdateConfigurationAsync(Dictionary<string, object?> properties)
    {
        await WriteConfigurationAsync(properties);
    }

    public async Task ClearConfigurationAsync()
    {
        await _configuration.ClearAsync();
        _logger.LogInformation("Cleared configuration store of resource {@ResourceId}", Id);
    }

    public async Task UpdateStateAsync(Dictionary<string, object?> state)
    {
        await WriteStateAsync(state);

        if (_behaviour is null) throw new InvalidOperationException("Behavior cannot be null");
        var deviceBehaviour = (IWritableDeviceBehaviour)_behaviour;
        await deviceBehaviour.OnPushStateAsync(state);

        _logger.LogInformation("Applied new state for device {@DeviceId}", Id);
    }

    public async Task ClearStateAsync()
    {
        await _state.ClearAsync();
        _logger.LogInformation("Cleared state of resource {@ResourceId}", Id);
    }

    public async ValueTask<Dictionary<string, object?>> GetConfiguration()
    {
        _logger.LogDebug("Retrieved configuration of resource {@ResourceId}", Id);
        return await _configuration.Get();
    }

    public async ValueTask<Dictionary<string, object?>> GetState()
    {
        _logger.LogDebug("Retrieved state of resource {@ResourceId}", Id);
        return await _state.Get();
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
        var context = new DeviceContext(Id, Metadata.State.ToImmutableDictionary(),
            configuration.ToImmutableDictionary(),
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
        _timer = RegisterTimer(ExecuteTimerBehaviourAsync, null, timeSpan, timeSpan);
    }

    private async Task WriteConfigurationAsync(Dictionary<string, object?> properties)
    {
        await _configuration.UpdateAsync(properties);

        Metadata.State["configuration_updated_at"] = DateTime.Now;
        await Metadata.WriteStateAsync();

        await EventBook.RegisterEventAsync(EventKind.UpdatedResource, Id);
        _logger.LogInformation("Wrote configuration to resource store {@ResourceId}", Id);
    }

    private async Task WriteConfigurationConditionalAsync(Dictionary<string, object?> configuration)
    {
        var storedConfiguration = await _configuration.Get();
        if (storedConfiguration.ContainsAll(configuration)) return;

        await _configuration.UpdateAsync(configuration);
    }

    private async Task WriteStateAsync(Dictionary<string, object?> state)
    {
        await _state.UpdateAsync(state);

        Metadata.State["state_updated_at"] = DateTime.Now;
        await Metadata.WriteStateAsync();

        await EventBook.RegisterEventAsync(EventKind.UpdatedResource, Id);

        _logger.LogInformation("Wrote state for device {@DeviceId}", Id);
    }

    private async Task WriteStateConditionalAsync(Dictionary<string, object?> state)
    {
        var storedState = await _state.Get();
        if (storedState.ContainsAll(state)) return;

        await WriteStateAsync(state);
    }

    public async Task ClearStoreAsync()
    {
        _timer?.Dispose();
        await _behaviourId.ClearStateAsync();
        await ClearMetadataAsync();
        await ClearConfigurationAsync();
        await ClearStateAsync();
        
        _logger.LogInformation("Cleared Device {@DeviceGrainId} store", Id);
    }
}