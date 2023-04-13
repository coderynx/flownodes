using System.Collections.Immutable;
using Flownodes.Sdk.Entities;
using Flownodes.Sdk.Resourcing;
using Flownodes.Sdk.Resourcing.Behaviours;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Worker.Extendability;
using Flownodes.Worker.Resourcing.Persistence;
using Orleans.Runtime;
using OrleansCodeGen.OrleansDashboard.Model;

namespace Flownodes.Worker.Resourcing;

[GrainType(FlownodesEntityNames.DataSource)]
internal sealed class DataSourceGrain : ResourceGrain, IDataSourceGrain
{
    private readonly IPersistentState<BehaviourId> _behaviourId;
    private readonly IJournaledStoreGrain<Dictionary<string, object?>> _configuration;
    private readonly IExtensionProvider _extensionProvider;
    private readonly ILogger<DataSourceGrain> _logger;
    private IDataSourceBehaviour? _behaviour;

    public DataSourceGrain(ILogger<DataSourceGrain> logger, IPersistentStateFactory stateFactory,
        IGrainContext grainContext, IExtensionProvider extensionProvider)
        : base(logger, stateFactory, grainContext)
    {
        _extensionProvider = extensionProvider;
        _configuration =
            GrainFactory.GetGrain<IJournaledStoreGrain<Dictionary<string, object?>>>($"{Id}_configuration");
        _behaviourId =
            stateFactory.Create<BehaviourId>(grainContext, new PersistentStateAttribute("resourceBehaviourId"));
        _logger = logger;
    }

    public async ValueTask<DataSourceResult> GetDataAsync(string actionId,
        Dictionary<string, object?>? parameters = null)
    {
        if (_behaviour is null) throw new NullReferenceException("Behaviour should not be null");

        var data = await _behaviour.GetDataAsync(actionId, parameters);
        return new DataSourceResult(data);
    }

    public async ValueTask<ResourceSummary> GetSummary()
    {
        var properties = new Dictionary<string, object?>
        {
            { "configuration", await _configuration.Get() }
        };
        
        return new ResourceSummary(Id, Metadata.State, properties);
    }

    public async Task UpdateBehaviourId(string behaviourId)
    {
        _behaviourId.State.Value = behaviourId;
        await _behaviourId.WriteStateAsync();
        await OnUpdateBehaviourAsync();

        _logger.LogInformation("Updated BehaviourId of ResourceGrain {@ResourceId}", Id);
    }

    public async Task UpdateConfigurationAsync(Dictionary<string, object?> configuration)
    {
        await _configuration.UpdateAsync(configuration);
        _logger.LogInformation("Updated DataSourceGrain {@DataSourceGrainId} configuration", Id);
    }

    public async Task ClearConfigurationAsync()
    {
        await _configuration.ClearAsync();
        _logger.LogInformation("Cleared DataSourceGrain {@DataSourceGrainId} configuration", Id);
    }

    private async Task OnUpdateBehaviourAsync()
    {
        if (_behaviourId.State.Value is null) return;

        var configuration = await _configuration.Get();
        var context =
            new DataSourceContext(Id, Metadata.State.ToImmutableDictionary(), configuration.ToImmutableDictionary());

        _behaviour =
            _extensionProvider.ResolveBehaviour<IDataSourceBehaviour, DataSourceContext>(_behaviourId.State.Value,
                context);
        await _behaviour.OnSetupAsync();
    }
}