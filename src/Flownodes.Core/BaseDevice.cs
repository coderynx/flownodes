using Flownodes.Core.Interfaces;
using Flownodes.Core.Models;
using Microsoft.Extensions.Logging;

namespace Flownodes.Core;

public abstract class BaseDevice : IBehaviour
{
    protected readonly ILogger<BaseDevice> Logger;

    protected BaseDevice(ILogger<BaseDevice> logger)
    {
        Logger = logger;
    }

    public virtual Task OnSetupAsync(ResourceContext context)
    {
        Logger.LogInformation("Initialized behaviour {BehaviourId}", context.Configuration?.BehaviourId);
        return Task.CompletedTask;
    }

    public virtual Task OnStateChangeAsync(Dictionary<string, object?> newState, ResourceContext context)
    {
        Logger.LogInformation("Updated state with {BehaviourId}", context.Configuration?.BehaviourId);
        return Task.CompletedTask;
    }
}