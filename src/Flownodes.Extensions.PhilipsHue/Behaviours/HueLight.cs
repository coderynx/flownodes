using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Flownodes.Sdk.Resourcing;
using Flownodes.Sdk.Resourcing.Attributes;
using Flownodes.Sdk.Resourcing.Behaviours;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Flownodes.Extensions.PhilipsHue.Behaviours;

[BehaviourId("hue_light")]
[BehaviourDescription("Device behaviour for the Philips Hue Light")]
public class HueLight : IReadableDeviceBehaviour, IWritableDeviceBehaviour
{
    private readonly DeviceContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<HueLight> _logger;

    public HueLight(IConfiguration configuration, ILogger<HueLight> logger,
        IHttpClientFactory httpClientFactory, DeviceContext context)
    {
        _logger = logger;
        _context = context;
        _httpClient = httpClientFactory.CreateClient();

        var bridgeAddress = configuration["PhilipsHue:HueBridge:Address"];
        var bridgeToken = configuration["PhilipsHue:HueBridge:Token"];

        var url = $"{bridgeAddress}/api/{bridgeToken}/";
        _httpClient.BaseAddress = new Uri(url);
    }

    public async Task<UpdateResourceBag> OnSetupAsync()
    {
        var lightId = _context.Configuration!["lightId"]?.ToString();

        var response = await _httpClient.GetAsync($"lights/{lightId}");

        if (!response.IsSuccessStatusCode) return new UpdateResourceBag();

        var json = await response.Content.ReadFromJsonAsync<JsonNode>();
        var bag = new UpdateResourceBag
        {
            Metadata =
            {
                ["name"] = json?["name"]?.GetValue<string>(),
                ["model_id"] = json?["modelid"]?.GetValue<string>(),
                ["manufacturer_name"] = json?["manufacturername"]?.GetValue<string>(),
                ["product_name"] = json?["productname"]?.GetValue<string>()
            },
            State =
            {
                ["reachable"] = json?["state"]?["reachable"]?.GetValue<bool>()
            }
        };

        _logger.LogInformation("Setup device {@DeviceId} as HueLight with ID {@HueLightId}", _context.Id,
            lightId);

        return bag;
    }

    public async Task<UpdateResourceBag> OnPullStateAsync()
    {
        var lightId = _context.Configuration!["lightId"]?.ToString();

        var response = await _httpClient.GetAsync($"lights/{lightId}");
        var bag = new UpdateResourceBag();

        _logger.LogInformation("Pulled state from HueLight device {@DeviceId} with ID {@HueLightId}",
            _context.Id, lightId);

        if (!response.IsSuccessStatusCode) return bag;

        var json = await response.Content.ReadFromJsonAsync<JsonNode>();
        bag.State["power"] = json?["state"]?["on"]?.GetValue<bool>();
        bag.State["reachable"] = json?["state"]?["reachable"]?.GetValue<bool>();

        return bag;
    }

    public async Task OnPushStateAsync(Dictionary<string, object?> newState)
    {
        var lightId = _context.Configuration!["lightId"]?.ToString();
        ArgumentException.ThrowIfNullOrEmpty(lightId);

        if (newState.TryGetValue("power", out var value))
        {
            var request = $"{{\"on\": {value?.ToString()?.ToLower()}}}";
            var response = await _httpClient.PutAsync("lights/1/state", new StringContent(request));
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode && content.Contains("success"))
                _logger.LogInformation("Pushed power state to HueLight device {@DeviceId} with ID {@HueLightId}",
                    _context.Id, lightId);
            else
                _logger.LogError("Failed to update light power state of {LightId} to on", lightId);
        }
    }
}