using Flownodes.Core.Interfaces;

namespace Flownodes.Worker.Services;

public interface IBehaviourProvider
{
    IDevice? GetDeviceBehaviour(string id);
    IDataCollectorBehaviour GetDataCollectorBehaviour(string id);
}