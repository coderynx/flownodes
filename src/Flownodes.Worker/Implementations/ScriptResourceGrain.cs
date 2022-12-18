using Flownodes.Core.Interfaces;
using Flownodes.Worker.Models;
using Flownodes.Worker.Services;
using Microsoft.ClearScript.V8;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

public interface IScriptContext
{
    void LogInformation(string text);
    void LogWarning(string text);
    void LogError(string text);
    void LogCritical(string text);
}

public class ScriptContext : IScriptContext
{
    private readonly ILogger<ScriptContext> _logger;

    public ScriptContext(ILogger<ScriptContext> logger)
    {
        _logger = logger;
    }

    public void LogInformation(string text)
    {
        _logger.LogInformation(text);
    }

    public void LogWarning(string text)
    {
        _logger.LogWarning(text);
    }

    public void LogError(string text)
    {
        _logger.LogError(text);
    }

    public void LogCritical(string text)
    {
        _logger.LogCritical(text);
    }
}

public class ScriptResourceGrain : ResourceGrain, IScriptResourceGrain
{
    private readonly IScriptContext _scriptContext;
    private readonly V8ScriptEngine _scriptEngine;
    
    public ScriptResourceGrain(ILogger<ScriptResourceGrain> logger,
        [PersistentState("scriptStore", "flownodes")]
        IPersistentState<ResourcePersistence> persistence,
        IEnvironmentService environmentService, IBehaviourProvider behaviourProvider, IScriptContext scriptContext, V8ScriptEngine scriptEngine) : base(logger, persistence,
        environmentService, behaviourProvider)
    {
        _scriptContext = scriptContext;
        _scriptEngine = scriptEngine;
    }

    public Task ExecuteAsync(Dictionary<string, object?>? parameters = null)
    {
        _scriptEngine.AddRestrictedHostObject("context", _scriptContext);
        _scriptEngine.Execute(Configuration["code"].ToString());

        Logger.LogInformation("Executed script with {Frn}", Frn);
        return Task.CompletedTask;
    }
}