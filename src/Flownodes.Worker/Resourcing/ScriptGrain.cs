using CSScriptLib;
using Flownodes.Sdk.Entities;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Shared.Resourcing.Scripts;
using Orleans.Runtime;

namespace Flownodes.Worker.Resourcing;

[GenerateSerializer]
internal sealed record ScriptStore
{
    [Id(0)] public string? Code { get; set; }
}

[GrainType(FlownodesEntityNames.Script)]
internal sealed class ScriptGrain : ResourceGrain, IScriptGrain
{
    private readonly ILogger<ScriptGrain> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IPersistentState<ScriptStore> _store;
    private IScript? _script;

    public ScriptGrain(ILogger<ScriptGrain> logger, ILoggerFactory loggerFactory,
        [PersistentState("scriptMetadata")] IPersistentState<Dictionary<string, object?>> metadata,
        [PersistentState("scriptStore")] IPersistentState<ScriptStore> store) :
        base(logger, metadata)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _store = store;
    }

    public async Task ExecuteAsync(Dictionary<string, object?>? parameters = null)
    {
        if (_store.State.Code is not null)
        {
            _logger.LogError("Could not load code from resource configuration");
            return;
        }

        if (_script is null)
        {
            _script = CSScript.Evaluator.LoadCode<IScript>(_store.State.Code);
            var logger = _loggerFactory.CreateLogger<FlownodesContext>();
            _script.Context = new FlownodesContext(logger, ResourceManager, AlertManager, Id);
        }

        await _script.ExecuteAsync(parameters);

        _logger.LogInformation("Executed script {@ScriptId}", Id);
    }

    public ValueTask<ResourceSummary> GetSummary()
    {
        var properties = new Dictionary<string, object?>
        {
            { "code", _store.State.Code }
        };

        return ValueTask.FromResult(new ResourceSummary(Id, Metadata.State, properties));
    }

    public async Task ClearStoreAsync()
    {
        await ClearMetadataAsync();
        await _store.ClearStateAsync();
        
        _logger.LogInformation("Cleared Script {@ScriptGrainId} store", Id);
    }

    public async Task UpdateCodeAsync(string code)
    {
        _store.State.Code = code;
        await _store.WriteStateAsync();

        _logger.LogInformation("Updated code of ScriptGrain {@ScriptGrainId}", Id);
    }
}