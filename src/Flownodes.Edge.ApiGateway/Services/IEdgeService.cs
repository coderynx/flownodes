using Flownodes.Edge.Core.Alerting;
using Flownodes.Edge.Core.Resources;

namespace Flownodes.Edge.ApiGateway.Services;

public interface IEdgeService
{
    IResourceManagerGrain GetResourceManager();
    IAlerterGrain GetAlerter();
}