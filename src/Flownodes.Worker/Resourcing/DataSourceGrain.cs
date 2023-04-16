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

[GrainType(EntityNames.DataSource)]
internal sealed class DataSourceGrain : ResourceGrain, IDataSourceGrain
{
    private readonly IPersistentState<BehaviourId> _behaviourId;
    private readonly IJournaledStoreGrain<Dictionary<string, object?>> _configuration;
    private readonly IExtensionProvider _extensionProvider;
    private readonly ILogger<DataSourceGrain> _logger;
    private IDataSourceBehaviour? _behaviour;

    public DataSourceGrain(ILogger<DataSourceGrain> logger, IExtensionProvider extensionProvider,
        [PersistentState("dataSourceMetadata")] IPersistentState<Dictionary<string, object?>> metadata,
        [PersistentState("behaviourId")] IPersistentState<BehaviourId> behaviourId)
        : base(logger, metadata)
    {
        _extensionProvider = extensionProvider;
        _logger = logger;
        _behaviourId = behaviourId;
        _configuration =
            GrainFactory.GetGrain<IJournaledStoreGrain<Dictionary<string, object?>>>($"{Id}_configuration");
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

    public async Task ClearStoreAsync()
    {
        await ClearMetadataAsync();
        await _behaviourId.ClearStateAsync();
        await _configuration.ClearAsync();
        
        _logger.LogInformation("Cleared DataSource {@DataSourceGrainId} store", Id);
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