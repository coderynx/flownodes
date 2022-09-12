using Flownodes.Cluster.Core.Resources;

namespace Flownodes.Cluster.ApiGateway.Endpoints.GetDevice;

public record GetDeviceResponse(DateTime Time, ResourceIdentityCard IdentityCard);