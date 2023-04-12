using System.Collections.Immutable;
using Flownodes.Sdk.Entities;
using Flownodes.Sdk.Resourcing;
using Flownodes.Sdk.Resourcing.Behaviours;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Worker.Extendability;
using Flownodes.Worker.Resourcing.Persistence;
using Orleans.Runtime;

namespace Flownodes.Worker.Resourcing;

[GrainType(FlownodesEntityNames.DataSource)]
internal sealed class DataSourceGrain : ResourceGrain, IDataSourceGrain
{
    private readonly IExtensionProvider _extensionProvider;
    private IDataSourceBehaviour? _behaviour;
    private readonly IPersistentState<BehaviourId> _behaviourId;
    private readonly ILogger<DataSourceGrain> _logger;

    public DataSourceGrain(ILogger<DataSourceGrain> logger, IPersistentStateFactory stateFactory,
        IGrainContext grainContext, IExtensionProvider extensionProvider)
        : base(logger, stateFactory, grainContext)
    {
        _extensionProvider = extensionProvider;
        _behaviourId =
            stateFactory.Create<BehaviourId>(grainContext, new PersistentStateAttribute("resourceBehaviourIdStore"));
        _logger = logger;
    }

    public async ValueTask<DataSourceResult> GetDataAsync(string actionId,
        Dictionary<string, object?>? parameters = null)
    {
        if (_behaviour is null) throw new NullReferenceException("Behaviour should not be null");

        var data = await _behaviour.GetDataAsync(actionId, parameters);
        return new DataSourceResult(data);
    }

    public async ValueTask<BaseResourceSummary> GetSummary()
    {
        return new DataSourceSummary(Id, Metadata, await GetState());
    }

    private async Task OnUpdateBehaviourAsync()
    {
        if (_behaviourId.State.Value is null) return;

        var configuration = await GetConfiguration();
        var context =
            new DataSourceContext(Id, Metadata.ToImmutableDictionary(), configuration.ToImmutableDictionary());

        _behaviour =
            _extensionProvider.ResolveBehaviour<IDataSourceBehaviour, DataSourceContext>(_behaviourId.State.Value,
                context);
        await _behaviour.OnSetupAsync();
    }

    public async Task UpdateBehaviourId(string behaviourId)
    {
        _behaviourId.State.Value = behaviourId;
        await _behaviourId.WriteStateAsync();
        await OnUpdateBehaviourAsync();

        _logger.LogInformation("Updated BehaviourId of ResourceGrain {@ResourceId}", Id);
    }
}