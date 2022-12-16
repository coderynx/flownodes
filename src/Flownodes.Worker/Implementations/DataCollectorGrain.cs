using Ardalis.GuardClauses;
using Flownodes.Core.Interfaces;
using Flownodes.Core.Models;
using Flownodes.Worker.Extensions;
using Flownodes.Worker.Models;
using Flownodes.Worker.Services;
using Newtonsoft.Json.Linq;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

public class DataCollectorGrain : Grain, IDataCollectorGrain
{
    private readonly IAlerterGrain _alerter;
    private readonly IBehaviourProvider _behaviourProvider;
    private readonly ILogger<DataCollectorGrain> _logger;
    private readonly IPersistentState<ResourcePersistence> _persistence;
    private IDataCollectorBehaviour? _behaviour;

    public DataCollectorGrain(IBehaviourProvider behaviourProvider,
        [PersistentState("dataCollectorPersistence", "flownodes")]
        IPersistentState<ResourcePersistence> persistence,
        ILogger<DataCollectorGrain> logger, IEnvironmentService environmentService)
    {
        _behaviourProvider = behaviourProvider;
        _persistence = persistence;
        _logger = logger;
        _alerter = environmentService.GetAlertManagerGrain();
    }

    private string Id => this.GetPrimaryKeyString();

    public async Task ConfigureAsync(string behaviorId, ResourceConfiguration? configuration = null,
        Dictionary<string, string>? metadata = null)
    {
        _behaviour = _behaviourProvider.GetDataCollectorBehaviour(behaviorId);
        Guard.Against.Null(_behaviour, nameof(_behaviour));

        _persistence.State.BehaviourId = behaviorId;
        _persistence.State.CreatedAt = DateTime.Now;
        _persistence.State.Configuration = configuration ?? new ResourceConfiguration();
        _persistence.State.Metadata = metadata ?? new Dictionary<string, string>();
        _persistence.State.State = new ResourceState();
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Configured data collector {DataCollectorGrainId}", Id);
    }

    public async Task<object> CollectAsync(string actionId, Dictionary<string, object?>? parameters = null)
    {
        EnsureConfiguration();
        Guard.Against.Null(_behaviour, nameof(_behaviour));

        object? result;
        if (parameters is null)
        {
            result = await _behaviour.UpdateAsync(actionId, _persistence.State.Configuration.Dictionary);
        }
        else
        {
            var newParams = new Dictionary<string, object?>(_persistence.State.Configuration.Dictionary);
            newParams.MergeInPlace(parameters);
            result = await _behaviour.UpdateAsync(actionId, newParams);
        }

        return result is null ? null : JToken.FromObject(result);
    }

    public Task<string> GetFrn()
    {
        EnsureConfiguration();
        return Task.FromResult($"frn:flownodes:data_collector:{_persistence.State.BehaviourId}:{Id}");
    }

    public async Task SelfRemoveAsync()
    {
        await _persistence.ClearStateAsync();
        _logger.LogInformation("Clear state for data collector {DataCollectorId}", Id);
    }

    private void EnsureConfiguration()
    {
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