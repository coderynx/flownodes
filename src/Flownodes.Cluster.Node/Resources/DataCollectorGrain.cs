using Autofac;
using Autofac.Extensions.DependencyInjection;
using Flownodes.Cluster.Core;
using Flownodes.Cluster.Core.Alerting;
using Flownodes.Cluster.Core.Resources;
using Flownodes.Cluster.Node.Extensions;
using Flownodes.Cluster.Node.Models;
using Newtonsoft.Json.Linq;
using Orleans;
using Orleans.Runtime;
using Throw;

namespace Flownodes.Cluster.Node.Resources;

public class DataCollectorGrain : Grain, IDataCollectorGrain
{
    private readonly IAlerterGrain _alerter;
    private readonly ILogger<DataCollectorGrain> _logger;
    private readonly IPersistentState<ResourcePersistence> _persistence;
    private readonly IResourceManagerGrain _resourceManager;
    private readonly IServiceProvider _serviceProvider;
    private IDataCollectorBehavior? _behavior;

    public DataCollectorGrain(IServiceProvider serviceProvider,
        [PersistentState("dataCollectorPersistence", "flownodes")]
        IPersistentState<ResourcePersistence> persistence,
        ILogger<DataCollectorGrain> logger, IGrainFactory grainFactory)
    {
        _serviceProvider = serviceProvider;
        _persistence = persistence;
        _logger = logger;

        _resourceManager = grainFactory.GetGrain<IResourceManagerGrain>(Globals.ResourceManagerGrainId);
        _alerter = grainFactory.GetGrain<IAlerterGrain>(Globals.AlerterGrainId);
    }

    private string Id => this.GetPrimaryKeyString();

    public async Task ConfigureAsync(string behaviorId, Dictionary<string, object?>? configuration = null,
        Dictionary<string, string>? metadata = null)
    {
        try
        {
            _behavior = _serviceProvider.GetAutofacRoot().ResolveKeyed<IDataCollectorBehavior>(behaviorId);
        }
        catch (Exception)
        {
            _logger.LogError("Could not find the given data collector behavior {BehaviorId}", behaviorId);
            throw;
        }

        _persistence.State.BehaviorId = behaviorId;
        _persistence.State.CreatedAt = DateTime.Now;
        _persistence.State.Configuration = configuration ?? new Dictionary<string, object?>();
        _persistence.State.Metadata = metadata ?? new Dictionary<string, string>();
        _persistence.State.State = new ResourceState();
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Configured data collector {DataCollectorGrainId}", Id);
    }

    public async Task<IAssetGrain?> CollectAsync(string actionId, Dictionary<string, object?>? parameters = null)
    {
        EnsureConfiguration();
        _behavior.ThrowIfNull();

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

        if (result is null) return null;

        var jToken = JToken.FromObject(result);

        var assetNamePath = _persistence.State.Configuration["assetNameJsonPath"]?.ToString();
        var assetName = assetNamePath is null ? null : $"{Id}_{jToken.SelectToken(assetNamePath)}";
        assetName.ThrowIfNull();

        var asset = await _resourceManager.GetAsset(assetName) ?? await _resourceManager.RegisterAssetAsync(assetName);
        await asset.UpdateAsync(new { from = _behavior.GetType().Name, data = result });

        _logger.LogInformation("Performed action {ActionId} of data collector {DataCollectorId}", actionId, Id);
        await ProduceInfoAlertAsync($"Performed action {actionId} of data collector {Id}");

        return asset;
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