using Ardalis.GuardClauses;
using Flownodes.Edge.Core.Alerting;
using Flownodes.Edge.Core.Resources;
using Flownodes.Edge.Node.Models;
using Orleans;
using Orleans.Runtime;

namespace Flownodes.Edge.Node.Resources;

internal class ResourceManagerGrain : Grain, IResourceManagerGrain
{
    private readonly IAlerterGrain _alerterGrain;
    private readonly IGrainFactory _grainFactory;

    private readonly ILogger<ResourceManagerGrain> _logger;
    private readonly IPersistentState<ResourceManagerPersistence> _persistence;

    public ResourceManagerGrain(ILogger<ResourceManagerGrain> logger,
        [PersistentState("resourceManagerState", "flownodes")]
        IPersistentState<ResourceManagerPersistence> persistence, IGrainFactory grainFactory)
    {
        _logger = logger;
        _persistence = persistence;
        _grainFactory = grainFactory;
        _alerterGrain = _grainFactory.GetGrain<IAlerterGrain>("alerter");
    }

    public async Task<IDeviceGrain> RegisterDeviceAsync(string id, string behaviorId,
        Dictionary<string, object?>? configuration = null)
    {
        Guard.Against.Null(id);
        Guard.Against.Null(behaviorId);

        if (_persistence.State.ResourceRegistrations.ContainsKey(id))
            throw new InvalidOperationException($"Device {id} is already registered");

        var grain = _grainFactory.GetGrain<IDeviceGrain>(id);

        try
        {
            await grain.ConfigureAsync(behaviorId, configuration);
        }
        catch (Exception)
        {
            _logger.LogError("Error configuring device {DeviceId}", id);
            throw;
        }

        _persistence.State.ResourceRegistrations.Add(id, behaviorId);
        await _persistence.WriteStateAsync();

        await _alerterGrain.ProduceInfoAlertAsync("frn:flownodes:resourceManager",
            $"Registered device {id} with behavior {behaviorId}");
        _logger.LogInformation("Registered device {DeviceId} with behavior {BehaviorId}", id, behaviorId);

        return grain;
    }

    public async Task<IDataCollectorGrain> RegisterDataCollectorAsync(string id, string behaviorId,
        Dictionary<string, object?>? configuration = null)
    {
        Guard.Against.NullOrWhiteSpace(id, nameof(id));
        Guard.Against.NullOrWhiteSpace(behaviorId, nameof(behaviorId));

        if (_persistence.State.ResourceRegistrations.ContainsKey(id))
            throw new InvalidOperationException($"Data collector {id} is already registered");

        var grain = _grainFactory.GetGrain<IDataCollectorGrain>(id);

        try
        {
            await grain.ConfigureAsync(behaviorId, configuration);
        }
        catch (Exception)
        {
            _logger.LogError("Error configuring data collector {DataCollectorId}", id);
            throw;
        }

        _persistence.State.ResourceRegistrations.Add(id, behaviorId);
        await _persistence.WriteStateAsync();

        await _alerterGrain.ProduceInfoAlertAsync("frn:flownodes:resourceManager",
            $"Registered data collector {id} with behavior {behaviorId}");
        _logger.LogInformation("Registered data collector {DataCollectorId} with behavior {BehaviorId}", id,
            behaviorId);

        return grain;
    }

    public async Task RemoveDeviceAsync(string id)
    {
        Guard.Against.NullOrWhiteSpace(id, nameof(id));

        if (!_persistence.State.ResourceRegistrations.ContainsKey(id))
            throw new KeyNotFoundException("The given device id was not found in the registry");

        var grain = _grainFactory.GetGrain<IDeviceGrain>(id);
        await grain.SelfRemoveAsync();

        var behaviorId = _persistence.State.ResourceRegistrations[id];

        _persistence.State.ResourceRegistrations.Remove(id);
        await _persistence.WriteStateAsync();

        await _alerterGrain.ProduceInfoAlertAsync("frn:flownodes:resourceManager",
            $"Removed device {id} with behavior {behaviorId}");
        _logger.LogInformation("Removed device {DeviceId} with behavior {BehaviorId}", id, behaviorId);
    }

    public async Task RemoveDataCollectorAsync(string id)
    {
        Guard.Against.NullOrWhiteSpace(id, nameof(id));

        if (!_persistence.State.ResourceRegistrations.ContainsKey(id))
            throw new KeyNotFoundException("The given data collector id was not found in the registry");

        var grain = _grainFactory.GetGrain<IDataCollectorGrain>(id);
        await grain.SelfRemoveAsync();

        var behaviorId = _persistence.State.ResourceRegistrations[id];

        _persistence.State.ResourceRegistrations.Remove(id);
        await _persistence.WriteStateAsync();

        await _alerterGrain.ProduceInfoAlertAsync("frn:flownodes:resourceManager",
            $"Removed data collector {id} with behavior {behaviorId}");
        _logger.LogInformation("Removed data collector {DataCollectorId} with behavior {BehaviorId}", id, behaviorId);
    }

    public async Task<IAssetGrain> RegisterAssetAsync(string id)
    {
        Guard.Against.Null(id, nameof(id));

        if (_persistence.State.ResourceRegistrations.ContainsKey(id))
            throw new InvalidOperationException($"Asset {id} is already registered");

        var grain = _grainFactory.GetGrain<IAssetGrain>(id);

        _persistence.State.ResourceRegistrations.Add(id, null);
        await _persistence.WriteStateAsync();

        await _alerterGrain.ProduceInfoAlertAsync("frn:flownodes:resourceManager",
            $"Registered asset {id}");
        _logger.LogInformation("Registered asset {AssetId}", id);

        return grain;
    }

    public async Task RemoveAssetAsync(string id)
    {
        Guard.Against.NullOrWhiteSpace(id, nameof(id));

        if (!_persistence.State.ResourceRegistrations.ContainsKey(id))
            throw new KeyNotFoundException("The given asset id was not found in the registry");

        var grain = _grainFactory.GetGrain<IAssetGrain>(id);
        await grain.SelfRemoveAsync();

        _persistence.State.ResourceRegistrations.Remove(id);
        await _persistence.WriteStateAsync();

        await _alerterGrain.ProduceInfoAlertAsync("frn:flownodes:resourceManager",
            $"Removed asset {id}");
        _logger.LogInformation("Removed asset {AssetId}", id);
    }

    public Task<List<IDeviceGrain>> GetDevices()
    {
        var grains = _persistence.State.ResourceRegistrations.Keys.Select(registration =>
            _grainFactory.GetGrain<IDeviceGrain>(registration)).ToList();

        _logger.LogDebug("Returning {DevicesCount} devices", grains.Count);
        return Task.FromResult(grains);
    }

    public Task<IDeviceGrain?> GetDevice(string id)
    {
        IDeviceGrain? grain = null;
        if (_persistence.State.ResourceRegistrations.ContainsKey(id))
        {
            grain = _grainFactory.GetGrain<IDeviceGrain>(id);

            _logger.LogDebug("Returning device {DeviceId}", id);
            return Task.FromResult<IDeviceGrain?>(grain);
        }

        _logger.LogError("Cannot find a device with ID {DeviceId}", id);
        return Task.FromResult(grain);
    }

    public Task<List<IDataCollectorGrain>> GetDataCollectors()
    {
        var grains = _persistence.State.ResourceRegistrations.Keys.Select(registration =>
            _grainFactory.GetGrain<IDataCollectorGrain>(registration)).ToList();

        _logger.LogDebug("Returning {DataCollectorsCount} data collectors", grains.Count);
        return Task.FromResult(grains);
    }

    public Task<IDataCollectorGrain?> GetDataCollector(string id)
    {
        IDataCollectorGrain? grain = null;
        if (_persistence.State.ResourceRegistrations.ContainsKey(id))
        {
            grain = _grainFactory.GetGrain<IDataCollectorGrain>(id);

            _logger.LogDebug("Returning data collector {DataCollectorId}", id);
            return Task.FromResult<IDataCollectorGrain?>(grain);
        }

        _logger.LogError("Cannot find a data collector with ID {DeviceId}", id);
        return Task.FromResult(grain);
    }

    public Task<List<IAssetGrain>> GetAssets()
    {
        var grains = _persistence.State.ResourceRegistrations.Select(registration =>
            _grainFactory.GetGrain<IAssetGrain>(registration.Key)).ToList();

        _logger.LogDebug("Returning {AssetsCount} assets", grains.Count);
        return Task.FromResult(grains);
    }

    public Task<IAssetGrain?> GetAsset(string id)
    {
        IAssetGrain? grain = null;
        if (_persistence.State.ResourceRegistrations.ContainsKey(id))
        {
            grain = _grainFactory.GetGrain<IAssetGrain>(id);

            _logger.LogDebug("Returning asset {AssetId}", id);
            return Task.FromResult<IAssetGrain?>(grain);
        }

        _logger.LogError("Cannot find an asset with ID {AssetId}", id);
        return Task.FromResult(grain);
    }

    public override Task OnActivateAsync()
    {
        _logger.LogInformation("Resource manager activated");
        return base.OnActivateAsync();
    }

    public override Task OnDeactivateAsync()
    {
        _logger.LogInformation("Resource manager deactivated");
        return base.OnDeactivateAsync();
    }
}