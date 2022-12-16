using Refit;

namespace Flownodes.CliClient.ApiSchemas;

public interface IDeviceApi
{
    [Post("/api/v1/devices")]
    Task DeployDevice(string deviceId, string behaviorId, Dictionary<string, string?> configuration);

    [Get("/api/v1/devices/{deviceId}")]
    Task GetDeviceInfoAsync(string deviceId);
}