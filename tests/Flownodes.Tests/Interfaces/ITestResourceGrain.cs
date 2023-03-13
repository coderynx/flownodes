using System.Threading.Tasks;
using Flownodes.Shared.Interfaces;

namespace Flownodes.Tests.Interfaces;

public interface ITestResourceGrain : IConfigurableResource, IStatefulResource
{
    ValueTask<TService> GetService<TService>() where TService : notnull;
}