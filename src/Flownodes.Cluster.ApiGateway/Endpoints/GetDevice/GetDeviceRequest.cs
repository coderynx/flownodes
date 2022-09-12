using Microsoft.AspNetCore.Mvc;

namespace Flownodes.Cluster.ApiGateway.Endpoints.GetDevice;

public class GetDeviceRequest
{
    [FromRoute] public string? Id { get; init; }
}