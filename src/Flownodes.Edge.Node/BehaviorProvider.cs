using Autofac;
using Autofac.Extensions.DependencyInjection;
using Flownodes.Edge.Core.Resources;

namespace Flownodes.Edge.Node;

public class BehaviorProvider : IBehaviorProvider
{
    private readonly ILifetimeScope _lifetimeScope;
    private readonly ILogger<BehaviorProvider> _logger;

    public BehaviorProvider(IServiceProvider serviceProvider, ILogger<BehaviorProvider> logger)
    {
        _logger = logger;
        _lifetimeScope = serviceProvider.GetAutofacRoot();
    }

    public IDeviceBehavior? GetDeviceBehavior(string id)
    {
        _logger.LogDebug("Retrieving device behavior {DeviceBehaviourId}", id);
        return _lifetimeScope.ResolveOptionalKeyed<IDeviceBehavior>(id);
    }

    public IDataCollectorBehavior GetDataCollectorBehavior(string id)
    {
        _logger.LogDebug("Retrieving data collector behavior {DataCollectorBehaviorId}", id);
        return _lifetimeScope.ResolveKeyed<IDataCollectorBehavior>(id);
    }
}