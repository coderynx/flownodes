using Flownodes.Sdk.Resourcing;
using Flownodes.Shared;
using Flownodes.Shared.Interfaces;
using Flownodes.Worker.Services;

namespace Flownodes.Worker.Implementations;

[GrainType(FlownodesObjectNames.DataSourceName)]
internal sealed class DataSourceGrain : ResourceGrain, IDataSourceGrain
{
    public DataSourceGrain(ILogger<DataSourceGrain> logger, IEnvironmentService environmentService,
        IPluginProvider pluginProvider)
        : base(logger, environmentService, pluginProvider)
    {
    }

    private new IDataSourceBehaviour? Behaviour => base.Behaviour as IDataSourceBehaviour;

    public async ValueTask<DataSourceResult> GetDataAsync(string actionId,
        Dictionary<string, object?>? parameters = null)
    {
        if (Behaviour is null) throw new NullReferenceException("Behaviour should not be null");

        var data = await Behaviour.GetDataAsync(actionId, parameters);
        return new DataSourceResult(data);
    }
}