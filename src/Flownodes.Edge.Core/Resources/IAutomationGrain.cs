using Orleans;

namespace Flownodes.Edge.Core.Resources;

public interface IAutomationGrain : IGrainWithStringKey
{
    Task ConfigureAsync();
}