using Flownodes.Sdk.Resourcing;
using Flownodes.Shared;
using Flownodes.Shared.Interfaces;
using Flownodes.Worker.Services;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

[GrainType(FlownodesObjectNames.DataSourceName)]
internal sealed class DataSourceGrain : ResourceGrain, IDataSourceGrain
{
    public DataSourceGrain(ILogger<DataSourceGrain> logger, IEnvironmentService environmentService,
        IPluginProvider pluginProvider, IPersistentStateFactory persistentStateFactory, IGrainContext grainContext)
        : base(logger, environmentService, pluginProvider, persistentStateFactory, grainContext)
    {
    }

    private new IDataSourceBehaviour? Behaviour => base.Behaviour as IDataSourceBehaviour;

    public async ValueTask<DataSourceResult> GetDataAsync(string actionId, Dictionary<string, object?>? parameters = null)
    {
        if (Behaviour is null) throw new NullReferenceException("Behaviour should not be null");

        var data = await Behaviour.GetDataAsync(actionId, parameters);
        return new DataSourceResult(data);
    }
}