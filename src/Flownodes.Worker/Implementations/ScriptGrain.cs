using CSScriptLib;
using Flownodes.Shared;
using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Scripting;
using Flownodes.Worker.Services;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

[GrainType(FlownodesObjectNames.ScriptName)]
internal sealed class ScriptGrain : ResourceGrain, IScriptGrain
{
    private readonly ILogger<ScriptGrain> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private IScript? _script;

    public ScriptGrain(ILogger<ScriptGrain> logger, IEnvironmentService environmentService,
        IPluginProvider pluginProvider, ILoggerFactory loggerFactory, IPersistentStateFactory persistentStateFactory,
        IGrainContext grainContext) :
        base(logger, environmentService, pluginProvider, persistentStateFactory, grainContext)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public async Task ExecuteAsync(Dictionary<string, object?>? parameters = null)
    {
        if (Configuration!.Properties["code"] is not string code)
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

        Logger.LogInformation("Executed script {ResourceId}", Id);
    }
}