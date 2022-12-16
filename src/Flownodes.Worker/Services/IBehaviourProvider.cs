using Flownodes.Core.Interfaces;

namespace Flownodes.Worker.Services;

public interface IBehaviourProvider
{
    IDeviceBehaviour? GetDeviceBehaviour(string id);
    IDataCollectorBehaviour GetDataCollectorBehaviour(string id);
}