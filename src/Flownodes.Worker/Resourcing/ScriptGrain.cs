using CSScriptLib;
using Flownodes.Sdk.Entities;
using Flownodes.Shared.Resourcing;
using Flownodes.Shared.Resourcing.Grains;
using Flownodes.Shared.Resourcing.Scripts;
using Flownodes.Worker.Resourcing.Persistence;
using Orleans.Runtime;

namespace Flownodes.Worker.Resourcing;

[GrainType(FlownodesEntityNames.Script)]
internal sealed class ScriptGrain : ResourceGrain, IScriptGrain
{
    private readonly ILogger<ScriptGrain> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private IScript? _script;

    public ScriptGrain(ILogger<ScriptGrain> logger, ILoggerFactory loggerFactory, IPersistentStateFactory stateFactory,
        IGrainContext grainContext) :
        base(logger, stateFactory, grainContext)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public async Task ExecuteAsync(Dictionary<string, object?>? parameters = null)
    {
        var configuration = await GetConfiguration();
        if (configuration["code"] is not string code)
        {
            _logger.LogError("Could not load code from resource configuration");
            return;
        }

        if (_script is null)
        {
            _script = CSScript.Evaluator.LoadCode<IScript>(code);
            var logger = _loggerFactory.CreateLogger<FlownodesContext>();
            _script.Context = new FlownodesContext(logger, ResourceManager, AlertManager, Id);
        }

        await _script.ExecuteAsync(parameters);

        _logger.LogInformation("Executed script {@ScriptId}", Id);
    }

    public async ValueTask<BaseResourceSummary> GetSummary()
    {
        var configuration = await GetConfiguration();
        return new ScriptSummary(Id, Metadata, configuration["code"] as string);
    }
}