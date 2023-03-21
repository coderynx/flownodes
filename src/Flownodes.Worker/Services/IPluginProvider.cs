using Flownodes.Sdk.Alerting;
using Flownodes.Sdk.Resourcing.Behaviours;

namespace Flownodes.Worker.Services;

public interface IPluginProvider
{
    IBehaviour? GetBehaviour(string id);
    IAlerterDriver? GetAlerterDriver(string id);
}