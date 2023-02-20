using Flownodes.Sdk.Resourcing;
using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;
using Flownodes.Worker.Services;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

public sealed class DataSourceGrain : ResourceGrain, IDataSourceGrain
{
    public DataSourceGrain(ILogger<DataSourceGrain> logger, IEnvironmentService environmentService,
        IBehaviourProvider behaviourProvider,
        [PersistentState("dataSourceConfigurationStore")]
        IPersistentState<ResourceConfigurationStore> configurationStore,
        [PersistentState("dataSourceMetadataStore")]
        IPersistentState<ResourceMetadataStore> metadataStore,
        [PersistentState("dataSourceStateStore")]
        IPersistentState<ResourceStateStore> stateStore)
        : base(logger, environmentService, behaviourProvider, configurationStore, metadataStore, stateStore)
    {
    }

    private new BaseDataSource? Behaviour => base.Behaviour as BaseDataSource;

    public async ValueTask<DataSourceResult> GetData(string actionId, Dictionary<string, object?>? parameters = null)
    {
        if (Behaviour is null) throw new NullReferenceException("Behaviour should not be null");

        var data = await Behaviour.GetDataAsync(actionId, parameters);
        return new DataSourceResult(data);
    }
}