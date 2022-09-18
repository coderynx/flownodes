using Microsoft.AspNetCore.Mvc;

namespace Flownodes.Edge.ApiGateway.Endpoints.GetDevice;

public class GetDeviceRequest
{
    [FromRoute] public string? Id { get; init; }
}