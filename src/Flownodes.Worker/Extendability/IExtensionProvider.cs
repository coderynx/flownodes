using Flownodes.Sdk.Alerting;
using Flownodes.Sdk.Resourcing;
using Flownodes.Sdk.Resourcing.Behaviours;

namespace Flownodes.Worker.Extendability;

public interface IExtensionProvider
{
    IBehaviour? GetBehaviour(string id, ResourceContext context);
    IAlerterDriver? GetAlerterDriver(string id);
    void BuildContainer();
}