using Flownodes.Core.Interfaces;
using Flownodes.Sdk.Resourcing;
using Flownodes.Worker.Extensions;
using Flownodes.Worker.Models;
using Flownodes.Worker.Services;
using MapsterMapper;
using Orleans.Runtime;

namespace Flownodes.Worker.Implementations;

public sealed class DeviceGrain : ResourceGrain, IDeviceGrain
{
    public DeviceGrain(IBehaviourProvider behaviourProvider,
        [PersistentState("devicePersistence", "flownodes")]
        IPersistentState<ResourcePersistence> persistence,
        ILogger<DeviceGrain> logger, IEnvironmentService environmentService, IMapper mapper) : base(logger, persistence,
        environmentService, behaviourProvider, mapper)
    {
    }

    private new BaseDevice? Behaviour => base.Behaviour as BaseDevice;

    public async Task UpdateStateAsync(Dictionary<string, object?> newState)
    {
        State.Dictionary.MergeInPlace(newState);
        await Persistence.WriteStateAsync();

        await SendStateAsync(newState);

        Logger.LogInformation("Updated state for {DeviceId}", Id);
    }

    private async Task SendStateAsync(Dictionary<string, object?> newState)
    {
        await Behaviour.OnStateChangeAsync(newState, GetResourceContext());

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