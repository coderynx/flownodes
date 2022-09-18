using Flownodes.Edge.Core.Resources;

namespace Flownodes.Edge.ApiGateway.Endpoints.GetDevice;

public record GetDeviceResponse(DateTime Time, ResourceIdentityCard IdentityCard);