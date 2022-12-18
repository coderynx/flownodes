using Ardalis.GuardClauses;
using Flownodes.Core.Interfaces;
using Flownodes.Worker.Extensions;
using Flownodes.Worker.Models;
using Flownodes.Worker.Services;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

public class DeviceGrain : ResourceGrain, IDeviceGrain
{
    public DeviceGrain(IBehaviourProvider behaviourProvider,
        [PersistentState("devicePersistence", "flownodes")]
        IPersistentState<ResourcePersistence> persistence,
        ILogger<DeviceGrain> logger, IEnvironmentService environmentService) : base(logger, persistence,
        environmentService, behaviourProvider)
    {
    }

    private string BehaviourId => Configuration.BehaviourId;
    private new IDevice? Behaviour => base.Behaviour as IDevice;

    public async Task UpdateStateAsync(Dictionary<string, object?> newState)
    {
        State.Dictionary.MergeInPlace(newState);
        await Persistence.WriteStateAsync();

        await SendStateAsync(newState);

        Logger.LogInformation("Updated state for {DeviceId}", Id);
    }

    private async Task SendStateAsync(Dictionary<string, object?> newState)
    {
        EnsureConfiguration();

        await Behaviour.OnStateChangeAsync(newState, Context);

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

    private void EnsureConfiguration()
    {
        Guard.Against.Null(base.Behaviour, nameof(base.Behaviour));
        Guard.Against.Null(BehaviourId, nameof(BehaviourId));
    }
}