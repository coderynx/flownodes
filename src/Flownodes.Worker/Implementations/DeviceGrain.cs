using Flownodes.Sdk.Resourcing;
using Flownodes.Shared.Interfaces;
using Flownodes.Worker.Models;
using Flownodes.Worker.Services;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

public sealed class DeviceGrain : ResourceGrain, IDeviceGrain
{
    public DeviceGrain(IBehaviourProvider behaviourProvider,
        [PersistentState("devicePersistence", "flownodes")]
        IPersistentState<ResourcePersistence> persistence,
        ILogger<DeviceGrain> logger, IEnvironmentService environmentService) : base(logger, persistence,
        environmentService, behaviourProvider)
    {
    }

    private new BaseDevice? Behaviour => base.Behaviour as BaseDevice;

    private async Task ExecuteTimerBehaviourAsync(object arg)
    {
        var context = GetResourceContext();
        await Behaviour?.OnUpdateAsync(context)!;

        // TODO: Evaluate if always writing causes performance issues.
        Persistence.State.StateStore.Properties = context.State.Properties;
        await Persistence.WriteStateAsync();

        Logger.LogDebug("Updated state for device {Id}: {State}", Id, StateStore.Properties);
    }

    protected override Task OnBehaviourUpdateAsync()
    {
        // Check if method is overriden
        var overridden = Behaviour.GetType().GetMethod("OnUpdateAsync")?.DeclaringType == Behaviour.GetType();
        if (overridden)
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