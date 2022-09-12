using Orleans;

namespace Flownodes.Cluster.Core.Resources;

public interface IAutomationGrain : IGrainWithStringKey
{
    Task ConfigureAsync();
}