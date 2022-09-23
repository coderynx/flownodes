using Flownodes.Edge.Core.Alerting;
using Flownodes.Edge.Core.Resources;
using Flownodes.Edge.Node.Extensions;
using Flownodes.Edge.Node.Models;
using Orleans;
using Orleans.Runtime;
using Throw;

namespace Flownodes.Edge.Node.Resources;

internal class DeviceGrain : Grain, IDeviceGrain
{
    private readonly IAlerterGrain _alerter;
    private readonly IBehaviorProvider _behaviorProvider;
    private readonly ILogger<DeviceGrain> _logger;
    private readonly IPersistentState<ResourcePersistence> _persistence;
    private IDeviceBehavior? _behavior;

    public DeviceGrain(IBehaviorProvider behaviorProvider,
        [PersistentState("devicePersistence", "flownodes")]
        IPersistentState<ResourcePersistence> persistence,
        ILogger<DeviceGrain> logger, IGrainFactory grainFactory)
    {
        _behaviorProvider = behaviorProvider;
        _persistence = persistence;
        _logger = logger;

        _alerter = grainFactory.GetGrain<IAlerterGrain>("alerter");
    }

    private string Id => this.GetPrimaryKeyString();

    public async Task<ResourceIdentityCard> GetIdentityCard()
    {
        EnsureConfiguration();

        var frn = await GetFrn();
        return new ResourceIdentityCard(frn, Id, _persistence.State.CreatedAt!.Value, _persistence.State.BehaviorId,
            _persistence.State.State.LastUpdate);
    }

    public async Task ConfigureAsync(string behaviorId, Dictionary<string, object?>? configuration = null,
        Dictionary<string, string>? metadata = null)
    {
        _logger.LogInformation("Configuring device {DeviceId} with behavior {BehaviorId}", Id, behaviorId);

        _behavior = _behaviorProvider.GetDeviceBehavior(behaviorId);
        _behavior.ThrowIfNull();

        _persistence.State.BehaviorId = behaviorId;
        _persistence.State.CreatedAt = DateTime.Now;
        _persistence.State.Configuration = configuration ?? new Dictionary<string, object?>();
        _persistence.State.Metadata = metadata ?? new Dictionary<string, string>();
        _persistence.State.State = new ResourceState();
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Configured device {DeviceId}", Id);
    }

    public async Task PerformAction(string actionName, Dictionary<string, object?>? parameters = null)
    {
        EnsureConfiguration();

        _behavior.ThrowIfNull();
        Dictionary<string, object?> result;
        if (parameters is null)
        {
            result = await _behavior.PerformAction(actionName, _persistence.State.Configuration);
        }
        else
        {
            var newParams = new Dictionary<string, object?>(_persistence.State.Configuration);
            newParams.MergeInPlace(parameters);
            result = await _behavior.PerformAction(actionName, newParams);
        }

        _persistence.State.State.Properties.MergeInPlace(result);
        _persistence.State.State.LastUpdate = DateTime.Now;
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Performed action {ActionName} of device {DeviceId}", actionName, Id);
        await ProduceInfoAlertAsync($"Performed action {actionName} of device {Id}");
    }

    public Task<object?> GetStateProperty(string key)
    {
        EnsureConfiguration();
        return Task.FromResult(_persistence.State.State.GetPropertyValue(key));
    }

    public Task<Dictionary<string, object?>> GetStateProperties()
    {
        EnsureConfiguration();
        return Task.FromResult(_persistence.State.State.Properties);
    }

    public Task<string> GetFrn()
    {
        EnsureConfiguration();
        return Task.FromResult($"frn:flownodes:device:{_persistence.State.BehaviorId}:{Id}");
    }

    public async Task SelfRemoveAsync()
    {
        await _persistence.ClearStateAsync();
        _logger.LogInformation("Clear state for device {DeviceId}", Id);
    }

    public override Task OnActivateAsync()
    {
        _logger.LogInformation("Activated device {DeviceId}", Id);
        return base.OnActivateAsync();
    }

    public override Task OnDeactivateAsync()
    {
        _logger.LogInformation("Deactivated device {DeviceId}", Id);
        return base.OnDeactivateAsync();
    }

    private void EnsureConfiguration()
    {
        _behavior.ThrowIfNull();
        _persistence.State.Configuration.ThrowIfNull();
        _persistence.State.Throw().IfNullOrWhiteSpace(x => x.BehaviorId);
        _persistence.State.Throw().IfNull(x => x.CreatedAt);
        _persistence.State.Throw().IfNull(x => x.Configuration);
        _persistence.State.Throw().IfNull(x => x.Metadata);
        _persistence.State.Throw().IfNull(x => x.State);
    }

    private async Task ProduceInfoAlertAsync(string message)
    {
        var frn = await GetFrn();
        await _alerter.ProduceInfoAlertAsync(frn, message);
    }

    private async Task ProduceWarningAlertAsync(string message)
    {
        var frn = await GetFrn();
        await _alerter.ProduceWarningAlertAsync(frn, message);
    }

    private async Task ProduceErrorAlertAsync(string message)
    {
        var frn = await GetFrn();
        await _alerter.ProduceErrorAlertAsync(frn, message);
    }
}