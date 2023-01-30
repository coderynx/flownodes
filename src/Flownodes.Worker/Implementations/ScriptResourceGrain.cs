using Flownodes.Shared.Interfaces;
using Flownodes.Shared.Models;
using Flownodes.Worker.Services;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

public sealed class ScriptResourceGrain : ResourceGrain, IScriptResourceGrain
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly V8ScriptEngine _scriptEngine = new(V8ScriptEngineFlags.EnableTaskPromiseConversion);

    public ScriptResourceGrain(ILogger<ScriptResourceGrain> logger, IEnvironmentService environmentService,
        IBehaviourProvider behaviourProvider, ILoggerFactory loggerFactory,
        [PersistentState("scriptResourceConfigurationStore")]
        IPersistentState<ResourceConfigurationStore> configurationStore,
        [PersistentState("scriptResourceMetadataStore")]
        IPersistentState<ResourceMetadataStore> metadataStore,
        [PersistentState("scriptResourceStateStore")]
        IPersistentState<ResourceStateStore> stateStore) :
        base(logger, environmentService, behaviourProvider, configurationStore, metadataStore, stateStore)
    {
        _loggerFactory = loggerFactory;
    }

    public Task ExecuteAsync(Dictionary<string, object?>? parameters = null)
    {
        var context = GetScriptContext();
        var code = Configuration["code"] as string;

        _scriptEngine.AddHostObject("host", new HostFunctions());
        _scriptEngine.AddHostObject("context", context);
        _scriptEngine.AddHostType(typeof(Console));
        _scriptEngine.AddHostType("deviceState", typeof(Dictionary<string, object?>));
        _scriptEngine.AddHostType(typeof(IDeviceGrain));
        _scriptEngine.Execute(code);

        Logger.LogInformation("Executed script {ResourceId}", Id);
        return Task.CompletedTask;
    }

    private ScriptContext GetScriptContext()
    {
        var logger = _loggerFactory.CreateLogger<ScriptContext>();
        return new ScriptContext(logger, ResourceManagerGrain);
    }
}