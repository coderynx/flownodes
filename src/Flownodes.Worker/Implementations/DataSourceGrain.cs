using Flownodes.Sdk.Resourcing;
using Flownodes.Shared.Interfaces;
using Flownodes.Worker.Models;
using Flownodes.Worker.Services;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

public sealed class DataSourceGrain : ResourceGrain, IDataSourceGrain
{
    public DataSourceGrain(ILogger<DataSourceGrain> logger,
        [PersistentState("dataSourceStore", "flownodes")]
        IPersistentState<ResourcePersistence> persistence, IEnvironmentService environmentService,
        IBehaviourProvider behaviourProvider) : base(logger, persistence, environmentService,
        behaviourProvider)
    {
    }

    private new BaseDataSource? Behaviour => base.Behaviour as BaseDataSource;

    public async ValueTask<DataSourceResult> GetData(string actionId, Dictionary<string, object?>? parameters = null)
    {
        var data = await Behaviour.GetDataAsync(actionId, parameters);
        return new DataSourceResult(data);
    }
}