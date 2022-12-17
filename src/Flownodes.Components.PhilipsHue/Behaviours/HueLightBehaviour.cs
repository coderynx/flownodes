using Ardalis.GuardClauses;
using Flownodes.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Flownodes.Components.PhilipsHue.Behaviours;

[BehaviourId("hue_light")]
[BehaviourDescription("Behaviour for the Philips Hue Light")]
public class HueLightBehaviour : IDeviceBehaviour
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HueLightBehaviour> _logger;

    public HueLightBehaviour(IConfiguration configuration, ILogger<HueLightBehaviour> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();

        var bridgeAddress = configuration["PhilipsHue:HueBridge:Address"];
        var bridgeToken = configuration["PhilipsHue:HueBridge:Token"];

        var url = $"{bridgeAddress}/api/{bridgeToken}/";
        _httpClient.BaseAddress = new Uri(url);
    }

    public async Task<Dictionary<string, object?>> PerformAction(BehaviourActionRequest request, BehaviourResourceContext context)
    {
        var lightId = context.Configuration["lightId"]?.ToString();
        Guard.Against.NullOrWhiteSpace(lightId, nameof(lightId));

        var result = new Dictionary<string, object?>();
        HttpResponseMessage response;
        string? content;

        switch (request.Id)
        {
            case "switch_on":
                response = await _httpClient.PutAsync("lights/1/state", new StringContent("{\"on\":true}"));
                content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && content.Contains("success"))
                {
                    context.State["power"] = true;
                    _logger.LogInformation("Updated light power state of {LightId} to on", lightId);
                }
                else
                {
                    _logger.LogError("Failed to update light power state of {LightId} to on", lightId);
                }

                break;

            case "switch_off":
                response = await _httpClient.PutAsync("lights/1/state", new StringContent("{\"on\":false}"));
                content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && content.Contains("success"))
                {
                    context.State["power"] = false;
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