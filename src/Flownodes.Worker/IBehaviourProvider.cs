using Flownodes.Core.Resources;

namespace Flownodes.Worker;

public interface IBehaviourProvider
{
    IDeviceBehaviour? GetDeviceBehaviour(string id);
    IDataCollectorBehaviour GetDataCollectorBehaviour(string id);
}