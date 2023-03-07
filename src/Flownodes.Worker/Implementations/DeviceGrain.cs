using Flownodes.Sdk.Resourcing;
using Flownodes.Shared.Interfaces;
using Flownodes.Worker.Models;
using Flownodes.Worker.Services;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

internal sealed class DeviceGrain : ResourceGrain, IDeviceGrain
{
    public DeviceGrain(IPluginProvider pluginProvider, ILogger<DeviceGrain> logger,
        IEnvironmentService environmentService,
        [PersistentState("deviceConfigurationStore")]
        IPersistentState<ResourceConfigurationStore> configurationStore,
        [PersistentState("deviceMetadataStore")]
        IPersistentState<ResourceMetadataStore> metadataStore,
        [PersistentState("deviceStateStore")] IPersistentState<ResourceStateStore> stateStore) :
        base(logger, environmentService, pluginProvider, configurationStore, metadataStore, stateStore)
    {
    }

    private new BaseDevice? Behaviour => base.Behaviour as BaseDevice;

    private async Task ExecuteTimerBehaviourAsync(object arg)
    {
        var context = GetResourceContext();

        await Behaviour?.OnUpdateAsync(context)!;
        State.UpdateState(context.State); // TODO: Evaluate if there's a better way.

        Logger.LogDebug("Updated state for device {@DeviceId}: {@State}", Id, State.Properties);
    }

    protected override Task OnBehaviourUpdateAsync()
    {
        var isOverridden = Behaviour?
            .GetType()
            .GetMethod("OnUpdateAsync")?.DeclaringType == Behaviour?
            .GetType();

        var updateState = Configuration.Properties.GetValueOrDefault("updateStateTimeSpan") as int?;
        if (!isOverridden || updateState is null) return Task.CompletedTask;
        
        var timeSpan = TimeSpan.FromSeconds(updateState.Value);
        RegisterTimer(ExecuteTimerBehaviourAsync, null, timeSpan, timeSpan);
        return Task.CompletedTask;
    }

    protected override async Task OnStateChangedAsync(Dictionary<string, object?> newState)
    {
        await Behaviour?.OnStateChangeAsync(newState, GetResourceContext())!;
        Logger.LogInformation("Applied new state for device {DeviceId}", Id);
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Activated device {DeviceId}", Id);
        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        Logger.LogInformation("Deactivated device {DeviceId}", Id);
        return base.OnDeactivateAsync(reason, cancellationToken);
    }
}