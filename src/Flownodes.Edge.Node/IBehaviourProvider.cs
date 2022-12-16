using Flownodes.Edge.Core.Resources;

namespace Flownodes.Edge.Node;

public interface IBehaviourProvider
{
    IDeviceBehaviour? GetDeviceBehaviour(string id);
    IDataCollectorBehaviour GetDataCollectorBehaviour(string id);
}