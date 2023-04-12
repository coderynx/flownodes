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

    public ScriptGrain(ILogger<ScriptGrain> logger, ILoggerFactory loggerFactory, IPersistentStateFactory stateFactory,
        IGrainContext grainContext) :
        base(logger, stateFactory, grainContext)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _store = stateFactory.Create<ScriptStore>(grainContext, new PersistentStateAttribute("scriptStore"));
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

    public ValueTask<BaseResourceSummary> GetSummary()
    {
        return ValueTask.FromResult<BaseResourceSummary>(new ScriptSummary(Id, Metadata.State, _store.State.Code));
    }

    public async Task UpdateCodeAsync(string code)
    {
        _store.State.Code = code;
        await _store.WriteStateAsync();

        _logger.LogInformation("Updated code of ScriptGrain {@ScriptGrainId}", Id);
    }
}