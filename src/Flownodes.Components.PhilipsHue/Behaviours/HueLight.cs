using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Flownodes.Sdk.Resourcing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Flownodes.Components.PhilipsHue.Behaviours;

[BehaviourId("hue_light")]
[BehaviourDescription("Device behaviour for the Philips Hue Light")]
public class HueLight : BaseDevice
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HueLight> _logger;

    public HueLight(IConfiguration configuration, ILogger<HueLight> logger,
        IHttpClientFactory httpClientFactory) : base(logger)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();

        var bridgeAddress = configuration["PhilipsHue:HueBridge:Address"];
        var bridgeToken = configuration["PhilipsHue:HueBridge:Token"];

        var url = $"{bridgeAddress}/api/{bridgeToken}/";
        _httpClient.BaseAddress = new Uri(url);
    }

    public override async Task OnSetupAsync(ResourceContext context)
    {
        var lightId = context.Configuration["lightId"]?.ToString();

        var response = await _httpClient.GetAsync($"lights/{lightId}");

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadFromJsonAsync<JsonNode>();

            context.Metadata["name"] = json?["name"]?.GetValue<string>();
            context.Metadata["model_id"] = json?["modelid"]?.GetValue<string>();
            context.Metadata["manufacturer_name"] = json?["manufacturername"]?.GetValue<string>();
            context.Metadata["product_name"] = json?["productname"]?.GetValue<string>();

            context.State["reachable"] = json?["state"]?["reachable"]?.GetValue<bool>();
        }
    }

    public override async Task OnStateChangeAsync(Dictionary<string, object?> newState, ResourceContext context)
    {
        var lightId = context.Configuration["lightId"]?.ToString();
        ArgumentException.ThrowIfNullOrEmpty(lightId);

        if (newState.TryGetValue("power", out var value))
        {
            var request = $"{{\"on\": {value?.ToString()?.ToLower()}}}";
            var response = await _httpClient.PutAsync("lights/1/state", new StringContent(request));
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode && content.Contains("success"))
                _logger.LogInformation("Updated light power state of {LightId} to on", lightId);
            else
                _logger.LogError("Failed to update light power state of {LightId} to on", lightId);
        }
    }

    public override async Task OnUpdateAsync(ResourceContext context)
    {
        var lightId = context.Configuration["lightId"]?.ToString();

        var response = await _httpClient.GetAsync($"lights/{lightId}");

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadFromJsonAsync<JsonNode>();

            context.State["power"] = json?["state"]?["on"]?.GetValue<bool>();
            context.State["reachable"] = json?["state"]?["reachable"]?.GetValue<bool>();
        }
    }
}