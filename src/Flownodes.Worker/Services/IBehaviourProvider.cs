using Flownodes.Sdk.Resourcing;

namespace Flownodes.Worker.Services;

public interface IBehaviourProvider
{
    IBehaviour? GetBehaviour(string id);
}