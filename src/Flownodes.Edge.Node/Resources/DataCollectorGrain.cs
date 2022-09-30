using Ardalis.GuardClauses;
using Flownodes.Edge.Core;
using Flownodes.Edge.Core.Alerting;
using Flownodes.Edge.Core.Resources;
using Flownodes.Edge.Node.Extensions;
using Flownodes.Edge.Node.Models;
using Newtonsoft.Json.Linq;
using Orleans;
using Orleans.Runtime;

namespace Flownodes.Edge.Node.Resources;

public class DataCollectorGrain : Grain, IDataCollectorGrain
{
    private readonly IAlerterGrain _alerter;
    private readonly IBehaviorProvider _behaviorProvider;
    private readonly ILogger<DataCollectorGrain> _logger;
    private readonly IPersistentState<ResourcePersistence> _persistence;
    private readonly IResourceManagerGrain _resourceManager;
    private IDataCollectorBehavior? _behavior;

    public DataCollectorGrain(IBehaviorProvider behaviorProvider,
        [PersistentState("dataCollectorPersistence", "flownodes")]
        IPersistentState<ResourcePersistence> persistence,
        ILogger<DataCollectorGrain> logger, IGrainFactory grainFactory)
    {
        _behaviorProvider = behaviorProvider;
        _persistence = persistence;
        _logger = logger;

        _resourceManager = grainFactory.GetGrain<IResourceManagerGrain>(Globals.ResourceManagerGrainId);
        _alerter = grainFactory.GetGrain<IAlerterGrain>(Globals.AlerterGrainId);
    }

    private string Id => this.GetPrimaryKeyString();

    public async Task ConfigureAsync(string behaviorId, Dictionary<string, object?>? configuration = null,
        Dictionary<string, string>? metadata = null)
    {
        _behavior = _behaviorProvider.GetDataCollectorBehavior(behaviorId);
        Guard.Against.Null(_behavior, nameof(_behavior));

        _persistence.State.BehaviorId = behaviorId;
        _persistence.State.CreatedAt = DateTime.Now;
        _persistence.State.Configuration = configuration ?? new Dictionary<string, object?>();
        _persistence.State.Metadata = metadata ?? new Dictionary<string, string>();
        _persistence.State.State = new ResourceState();
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Configured data collector {DataCollectorGrainId}", Id);
    }

    public async Task<object> CollectAsync(string actionId, Dictionary<string, object?>? parameters = null)
    {
        EnsureConfiguration();
        Guard.Against.Null(_behavior, nameof(_behavior));

        object? result;
        if (parameters is null)
        {
            result = await _behavior.UpdateAsync(actionId, _persistence.State.Configuration);
        }
        else
        {
            var newParams = new Dictionary<string, object?>(_persistence.State.Configuration);
            newParams.MergeInPlace(parameters);
            result = await _behavior.UpdateAsync(actionId, newParams);
        }

        return result is null ? null : JToken.FromObject(result);
    }

    public Task<string> GetFrn()
    {
        EnsureConfiguration();
        return Task.FromResult($"frn:flownodes:data_collector:{_persistence.State.BehaviorId}:{Id}");
    }

    public async Task SelfRemoveAsync()
    {
        await _persistence.ClearStateAsync();
        _logger.LogInformation("Clear state for data collector {DataCollectorId}", Id);
    }

    private void EnsureConfiguration()
    {
        Guard.Against.Null(_persistence.State.BehaviorId, nameof(_persistence.State.BehaviorId));
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