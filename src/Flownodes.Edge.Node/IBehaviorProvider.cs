using Flownodes.Edge.Core.Resources;

namespace Flownodes.Edge.Node;

public interface IBehaviorProvider
{
    IDeviceBehavior? GetDeviceBehavior(string id);
    IDataCollectorBehavior GetDataCollectorBehavior(string id);
}