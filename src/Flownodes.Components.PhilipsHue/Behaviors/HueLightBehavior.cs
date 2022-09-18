using Flownodes.Cluster.Core.Resources;
using Flownodes.Components.PhilipsHue.ApiSchemas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Refit;
using Throw;

namespace Flownodes.Components.PhilipsHue.Behaviors;

public class HueLightBehavior : IDeviceBehavior
{
    private readonly IHueBridgeApi _hueBridgeApi;
    private readonly ILogger<HueLightBehavior> _logger;
    private readonly string _token;

    public HueLightBehavior(IConfiguration configuration, ILogger<HueLightBehavior> logger)
    {
        _logger = logger;
        _hueBridgeApi = RestService.For<IHueBridgeApi>(configuration["PhilipsHue:HueBridge:Address"]);
        _token = configuration["PhilipsHue:HueBridge:Token"];
    }

    public async Task<Dictionary<string, object?>> PerformAction(string actionId,
        Dictionary<string, object?>? parameters = null)
    {
        parameters.ThrowIfNull();

        var lightId = parameters["lightId"]?.ToString();
        lightId.ThrowIfNull();

        var result = new Dictionary<string, object?>();

        ApiResponse<string> response;
        switch (actionId)
        {
            case "switch-on":
                response = await _hueBridgeApi.UpdateHueLightState(_token, lightId, "{\"on\":true}");
                if (response.IsSuccessStatusCode && response.Content.Contains("success"))
                {
                    result.Add("power", "on");
                    _logger.LogInformation("Updated light power state of {LightId} to on", lightId);
                }
                else
                {
                    _logger.LogError("Failed to update light power state of {LightId} to on", lightId);
                }

                break;

            case "switch-off":
                response = await _hueBridgeApi.UpdateHueLightState(_token, lightId, "{\"on\":false}");
                if (response.IsSuccessStatusCode && response.Content.Contains("success"))
                {
                    result.Add("power", "off");
                    _logger.LogInformation("Updated light power state of {LightId} to off", lightId);
                }
                else
                {
                    _logger.LogError("Failed to update light power state of {LightId} to off", lightId);
                }

                break;
        }

        return result;
    }
}