using Flownodes.Sdk.Alerting;
using Flownodes.Sdk.Resourcing.Behaviours;

namespace Flownodes.Worker.Extendability;

public interface IComponentProvider
{
    IBehaviour? GetBehaviour(string id);
    IAlerterDriver? GetAlerterDriver(string id);
    void BuildContainer();
}