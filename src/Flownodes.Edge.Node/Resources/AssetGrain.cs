using Flownodes.Edge.Core.Alerting;
using Flownodes.Edge.Core.Resources;
using Flownodes.Edge.Node.Models;
using Flownodes.Edge.Node.Services;
using Newtonsoft.Json.Linq;
using Orleans.Runtime;

namespace Flownodes.Edge.Node.Resources;

public class AssetGrain : Grain, IAssetGrain
{
    private readonly IAlerterGrain _alerter;
    private readonly ILogger<AssetGrain> _logger;
    private readonly IPersistentState<AssetPersistence> _persistence;

    public AssetGrain([PersistentState("assetPersistence", "flownodes")] IPersistentState<AssetPersistence> persistence,
        ILogger<AssetGrain> logger, IGrainFactory grainFactory, IEnvironmentService environmentService)
    {
        _persistence = persistence;
        _logger = logger;
        _alerter = environmentService.GetAlertManagerGrain();
    }

    private string Id => this.GetPrimaryKeyString();

    public async Task UpdateAsync(object data)
    {
        var jObject = JObject.FromObject(data);
        _persistence.State.State.Data.Merge(jObject);
        _persistence.State.State.UpdatedAt = DateTime.Now;
        await _persistence.WriteStateAsync();

        _logger.LogInformation("Updated state for asset {AssetId}", Id);
    }

    public Task<JToken?> QueryData(string jsonPath)
    {
        var data = _persistence.State.State.Data;
        var result = data.SelectToken(jsonPath);
        return Task.FromResult(result);
    }

    public Task<string> GetFrn()
    {
        return Task.FromResult($"frn:flownodes:asset:{Id}");
    }

    public async Task SelfRemoveAsync()
    {
        await _persistence.ClearStateAsync();
        _logger.LogInformation("Clear state for data object {DataObjectId}", Id);
    }

    public async Task ProduceInfoAlertAsync(string message)
    {
        var frn = await GetFrn();
        await _alerter.ProduceInfoAlertAsync(frn, message);
    }

    public async Task ProduceWarningAlertAsync(string message)
    {
        var frn = await GetFrn();
        await _alerter.ProduceWarningAlertAsync(frn, message);
    }

    public async Task ProduceErrorAlertAsync(string message)
    {
        var frn = await GetFrn();
        await _alerter.ProduceErrorAlertAsync(frn, message);
    }
}