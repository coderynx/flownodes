using Flownodes.Cluster.Core.Alerting;
using Flownodes.Cluster.Core.Resources;

namespace Flownodes.Cluster.ApiGateway.Services;

public interface IEdgeService
{
    IResourceManagerGrain GetResourceManager();
    IAlerterGrain GetAlerter();
}