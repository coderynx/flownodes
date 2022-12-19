using Flownodes.Core.Interfaces;
using Flownodes.Core.Models;
using Microsoft.Extensions.Logging;

namespace Flownodes.Core;

public abstract class BaseDataSource : IBehaviour
{
    protected readonly ILogger<BaseDataSource> Logger;

    protected BaseDataSource(ILogger<BaseDataSource> logger)
    {
        Logger = logger;
    }

    public virtual Task OnSetupAsync(ResourceContext context)
    {
        Logger.LogInformation("Initialized base data source {BehaviourId}", context.Configuration.BehaviourId);
        return Task.CompletedTask;
    }

    public virtual async ValueTask<object?> GetDataAsync(string actionId,
        Dictionary<string, object?>? parameters = null)
    {
        throw new NotImplementedException();
    }
}