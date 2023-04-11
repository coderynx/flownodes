using Flownodes.Sdk.Alerting;
using Flownodes.Sdk.Resourcing;
using Flownodes.Sdk.Resourcing.Behaviours;

namespace Flownodes.Worker.Extendability;

public interface IExtensionProvider
{
    TBehaviour ResolveBehaviour<TBehaviour, TContext>(string id, TContext context) where TBehaviour : IBehaviour
        where TContext : ResourceContext;

    IAlerterDriver? GetAlerterDriver(string id);
    void BuildContainer();
}