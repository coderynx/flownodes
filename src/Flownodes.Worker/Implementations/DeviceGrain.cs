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
        State.Properties = context.State; // TODO: Evaluate if there's a better way.

        Logger.LogDebug("Updated state for device {Id}: {State}", Id, State.Properties);
    }

    protected override Task OnBehaviourUpdateAsync()
    {
        // Check if method is overriden
        var isOverridden = Behaviour?
            .GetType()
            .GetMethod("OnUpdateAsync")?.DeclaringType == Behaviour?
            .GetType();

        if (isOverridden)
            RegisterTimer(ExecuteTimerBehaviourAsync, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

        return base.OnBehaviourUpdateAsync();
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