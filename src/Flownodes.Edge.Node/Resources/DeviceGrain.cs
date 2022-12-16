using Ardalis.GuardClauses;
using Flownodes.Edge.Core.Alerting;
using Flownodes.Edge.Core.Resources;
using Flownodes.Edge.Node.Models;
using Orleans.Runtime;

namespace Flownodes.Edge.Node.Resources;

public class DeviceGrain : Grain, IDeviceGrain
{
    private readonly IAlerterGrain _alerter;
    private readonly IBehaviourProvider _behaviourProvider;
    private readonly ILogger<DeviceGrain> _logger;
    private readonly IPersistentState<ResourcePersistence> _persistence;
    private IDeviceBehaviour? _behaviourId;

    public DeviceGrain(IBehaviourProvider behaviourProvider,
        [PersistentState("devicePersistence", "flownodes")]
        IPersistentState<ResourcePersistence> persistence,
        ILogger<DeviceGrain> logger, IGrainFactory grainFactory)
    {
        _behaviourProvider = behaviourProvider;
        _persistence = persistence;
        _logger = logger;

        _alerter = grainFactory.GetGrain<IAlerterGrain>("alerter");
    }

    private string Id => this.GetPrimaryKeyString();

    public async Task<ResourceIdentityCard> GetIdentityCard()
    {
        EnsureConfiguration();

        var frn = await GetFrn();
        return new ResourceIdentityCard(frn, Id, _persistence.State.CreatedAt!.Value, _persistence.State.BehaviourId);
    }

    public async Task ConfigureAsync(string behaviorId, ResourceConfiguration configuration,
        Dictionary<string, string>? metadata = null)
    {
        _logger.LogInformation("Configuring device {DeviceId} with behaviour {BehaviourId}", Id, behaviorId);

        _behaviourId = _behaviourProvider.GetDeviceBehaviour(behaviorId);
        Guard.Against.Null(_behaviourId, nameof(_behaviourId));

        _persistence.State.BehaviourId = behaviorId;
        _persistence.State.CreatedAt = DateTime.Now;
        _persistence.State.Configuration = configuration;
        _persistence.State.Metadata = metadata ?? new Dictionary<string, string>();
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Configured device {DeviceId}", Id);
    }

    public async Task PerformAction(string actionName, Dictionary<string, object?>? parameters = null)
    {
        EnsureConfiguration();

        Guard.Against.Null(_behaviourId, nameof(_behaviourId));
        await _behaviourId.PerformAction(actionName, parameters, _persistence.State.Configuration,
            _persistence.State.State);
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Performed action {ActionName} of device {DeviceId}", actionName, Id);
        await ProduceInfoAlertAsync($"Performed action {actionName} of device {Id}");
    }

    public Task<object?> GetStateProperty(string key)
    {
        EnsureConfiguration();
        return Task.FromResult(_persistence.State.State.GetValue(key));
    }

    public Task<Dictionary<string, object?>> GetStateProperties()
    {
        EnsureConfiguration();
        return Task.FromResult(_persistence.State.State.Dictionary);
    }

    public Task<string> GetFrn()
    {
        EnsureConfiguration();
        return Task.FromResult($"frn:flownodes:device:{_persistence.State.BehaviourId}:{Id}");
    }

    public async Task SelfRemoveAsync()
    {
        await _persistence.ClearStateAsync();
        _logger.LogInformation("Clear state for device {DeviceId}", Id);
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activated device {DeviceId}", Id);
        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivated device {DeviceId}", Id);
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    private void EnsureConfiguration()
    {
        Guard.Against.Null(_behaviourId, nameof(_behaviourId));
        Guard.Against.Null(_persistence.State.BehaviourId, nameof(_persistence.State.BehaviourId));
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