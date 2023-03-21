using Flownodes.Sdk.Entities;
using Flownodes.Sdk.Resourcing.Behaviours;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Worker.Services;

namespace Flownodes.Worker.Resourcing;

[GrainType(FlownodesEntityNames.DataSource)]
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