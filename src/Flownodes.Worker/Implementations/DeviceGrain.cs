using Ardalis.GuardClauses;
using Flownodes.Core.Interfaces;
using Flownodes.Core.Models;
using Flownodes.Worker.Extensions;
using Flownodes.Worker.Models;
using Flownodes.Worker.Services;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

public class DeviceGrain : Grain, IDeviceGrain
{
    private readonly IAlerterGrain _alerter;
    private readonly IBehaviourProvider _behaviourProvider;
    private readonly ILogger<DeviceGrain> _logger;
    private readonly IPersistentState<ResourcePersistence> _persistence;
    private IDevice? _behaviour;

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

    public async Task SetupAsync(string behaviourId, ResourceConfiguration configuration,
        Dictionary<string, string>? metadata = null)
    {
        _logger.LogInformation("Configuring device {DeviceId} with behaviour {BehaviourId}", Id, behaviourId);

        _behaviour = _behaviourProvider.GetDeviceBehaviour(behaviourId);
        Guard.Against.Null(_behaviour, nameof(_behaviour));

        metadata ??= new Dictionary<string, string>();
        _persistence.State.Initialize(behaviourId, configuration, metadata);
        var context = new ResourceContext(_persistence.State.Configuration, _persistence.State.Metadata,
            _persistence.State.State);
        await _behaviour.OnSetupAsync(context);
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Configured device {DeviceId}", Id);
    }

    public ValueTask<ResourceConfiguration> GetConfiguration()
    {
        return ValueTask.FromResult(_persistence.State.Configuration);
    }

    public async Task UpdateStateAsync(Dictionary<string, object?> newState)
    {
        _persistence.State.State.Dictionary.MergeInPlace(newState);
        await _persistence.WriteStateAsync();

        await SendStateAsync(newState);

        _logger.LogInformation("Updates state for {DeviceId}", Id);
    }

    public async Task PerformAction(string id, Dictionary<string, object?>? parameters = null)
    {
        EnsureConfiguration();
        Guard.Against.Null(_behaviour, nameof(_behaviour));

        var request = new ActionRequest(id, parameters);
        var context = new ResourceContext(_persistence.State.Configuration, _persistence.State.Metadata,
            _persistence.State.State);

        await _behaviour.PerformAction(request, context);
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Performed action {ActionId} of device {DeviceId}", id, Id);
        await ProduceInfoAlertAsync($"Performed action {id} of device {Id}");
    }

    public ValueTask<Dictionary<string, string>> GetMetadata()
    {
        return ValueTask.FromResult(_persistence.State.Metadata);
    }

    public Task<object?> GetStateProperty(string key)
    {
        EnsureConfiguration();
        return Task.FromResult(_persistence.State.State.GetValue(key));
    }

    public Task<ResourceState> GetState()
    {
        EnsureConfiguration();
        return Task.FromResult(_persistence.State.State);
    }

    public Task<string> GetFrn()
    {
        EnsureConfiguration();
        return Task.FromResult($"frn:flownodes:device:{_persistence.State.BehaviourId}:{Id}");
    }

    public async Task RemoveAsync()
    {
        await _persistence.ClearStateAsync();
        _logger.LogInformation("Clear state for device {DeviceId}", Id);
    }

    private async Task SendStateAsync(Dictionary<string, object?> newState)
    {
        EnsureConfiguration();

        var context = new ResourceContext(_persistence.State.Configuration, _persistence.State.Metadata,
            _persistence.State.State);
        await _behaviour.OnStateChangeAsync(newState, context);

        _logger.LogInformation("Applied new state for device {DeviceId}", Id);
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
        Guard.Against.Null(_behaviour, nameof(_behaviour));
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