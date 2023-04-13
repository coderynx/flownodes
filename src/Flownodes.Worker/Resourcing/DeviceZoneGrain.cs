using Flownodes.Sdk.Entities;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Resourcing.Exceptions;
using Flownodes.Shared.Resourcing.Grains;
using Orleans.Runtime;

namespace Flownodes.Worker.Resourcing;

[GrainType(FlownodesEntityNames.DeviceZone)]
internal sealed class DeviceZoneGrain : ResourceGrain, IDeviceZoneGrain
{
    private readonly ILogger<DeviceZoneGrain> _logger;
    private readonly IPersistentState<HashSet<string>> _registrations;

    public DeviceZoneGrain(ILogger<DeviceZoneGrain> logger,
        IPersistentStateFactory stateFactory,
        IGrainContext context)
        : base(logger, stateFactory, context)
    {
        _logger = logger;
        _registrations =
            stateFactory.Create<HashSet<string>>(context, new PersistentStateAttribute("deviceZoneRegistrations"));
    }

    public async Task RegisterDeviceAsync(FlownodesId id)
    {
        if (!await ResourceManager.IsResourceRegistered(id.SecondName!)) throw new ResourceNotFoundException(id);
        if (_registrations.State.Contains(id)) throw new Exception($"Device {id} already registered into DeviceZone");

        _registrations.State.Add(id);
        await _registrations.WriteStateAsync();

        _logger.LogInformation("Registered device {@DeviceId} into zone {@DeviceZoneId}", id, Id);
    }

    public async Task UnregisterDeviceAsync(FlownodesId id)
    {
        _registrations.State.Remove(id);
        await _registrations.WriteStateAsync();

        _logger.LogInformation("Unregistered device {@DeviceId} from zone {@DeviceZoneId}", id, Id);
    }

    public async ValueTask<IDeviceGrain?> GetDeviceAsync(FlownodesId id)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        if (!await ResourceManager.IsResourceRegistered(id.SecondName!)) return null;
        if (!_registrations.State.Contains(id)) return null;
        return await ResourceManager.GetResourceAsync<IDeviceGrain>(id.SecondName!);
    }

    public ValueTask<HashSet<string>> GetRegistrations()
    {
        return ValueTask.FromResult(_registrations.State);
    }
    
    public async Task ClearRegistrationsAsync()
    {
        await _registrations.ClearStateAsync();
        _logger.LogInformation("Cleared registrations of DeviceZone {@DeviceZoneGrainId}", Id);
    }

    public ValueTask<ResourceSummary> GetSummary()
    {
        var properties = new Dictionary<string, object?>
        {
            { "registrations", _registrations.State }
        };

        return ValueTask.FromResult(new ResourceSummary(Id, Metadata.State, properties));
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activated DeviceZoneGrain {@DeviceZoneGrainId}", Id);
        return Task.CompletedTask;
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivated DeviceZoneGrain {@DeviceZoneGrainId} for reason {@DeactivationReason}", Id,
            reason.Description);
        return Task.CompletedTask;
    }
}