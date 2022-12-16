using Autofac;
using Autofac.Extensions.DependencyInjection;
using Flownodes.Core.Resources;

namespace Flownodes.Worker;

public class BehaviourProvider : IBehaviourProvider
{
    private readonly ILifetimeScope _lifetimeScope;
    private readonly ILogger<BehaviourProvider> _logger;

    public BehaviourProvider(IServiceProvider serviceProvider, ILogger<BehaviourProvider> logger)
    {
        _logger = logger;
        _lifetimeScope = serviceProvider.GetAutofacRoot();
    }

    public IDeviceBehaviour? GetDeviceBehaviour(string id)
    {
        _logger.LogDebug("Retrieving device behavior {DeviceBehaviourId}", id);
        return _lifetimeScope.ResolveOptionalKeyed<IDeviceBehaviour>(id);
    }

    public IDataCollectorBehaviour GetDataCollectorBehaviour(string id)
    {
        _logger.LogDebug("Retrieving data collector behavior {DataCollectorBehaviorId}", id);
        return _lifetimeScope.ResolveKeyed<IDataCollectorBehaviour>(id);
    }
}