using System.Threading.Tasks;
using Flownodes.Shared.Resourcing.Grains;

namespace Flownodes.Tests.Interfaces;

public interface ITestResourceGrain : IConfigurableResourceGrain, IStatefulResourceGrain
{
    ValueTask<TService> GetService<TService>() where TService : notnull;
}