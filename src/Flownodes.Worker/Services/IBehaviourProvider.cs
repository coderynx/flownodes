using Flownodes.Core.Interfaces;

namespace Flownodes.Worker.Services;

public interface IBehaviourProvider
{
    IBehaviour? GetBehaviour(string id);
}