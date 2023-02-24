using Microsoft.Extensions.Logging;

namespace Flownodes.Sdk.Resourcing;

public abstract class BaseDevice : IBehaviour
{
    protected readonly ILogger<BaseDevice> Logger;

    protected BaseDevice(ILogger<BaseDevice> logger)
    {
        Logger = logger;
    }

    public virtual Task OnSetupAsync(ResourceContext context)
    {
        Logger.LogInformation("Initialized behaviour {BehaviourId}", context.BehaviorId);
        return Task.CompletedTask;
    }

    public virtual Task OnStateChangeAsync(Dictionary<string, object?> newState, ResourceContext context)
    {
        Logger.LogInformation("Updated state with {BehaviourId}", context.BehaviorId);
        return Task.CompletedTask;
    }

    public virtual Task OnUpdateAsync(ResourceContext context)
    {
        Logger.LogInformation("Updating state from device with {BehaviourId}", context.BehaviorId);
        return Task.CompletedTask;
    }
}