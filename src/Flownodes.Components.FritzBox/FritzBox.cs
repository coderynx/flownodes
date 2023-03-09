using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Flownodes.Sdk.Resourcing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Flownodes.Components.FritzBox;

[BehaviourId("fritz_box")]
[BehaviourDescription("FritzBox behaviour for Flownodes.")]
public class FritzBox : IReadableDeviceBehaviour
{
    private readonly string? _address;
    private readonly HttpClient _httpClient;

    private readonly ILogger<FritzBox> _logger;
    private readonly string _password;
    private readonly string _username;
    private string? _sid;

    public FritzBox(ILogger<FritzBox> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();

        _address = configuration["FritzBox:Address"]
                   ?? throw new InvalidOperationException("FritzBox address not configured");
        _username = configuration["FritzBox:Username"]
                    ?? throw new InvalidOperationException("FritzBox username not configured");
        _password = configuration["FritzBox:Password"]
                    ?? throw new InvalidOperationException("FritzBox password not configured");

        var url = $"{_address}/";
        _httpClient.BaseAddress = new Uri(url);
    }

    public async Task OnSetupAsync(ResourceContext context)
    {
        _sid = GetSessionId(_username, _password);
        _logger.LogInformation("Successfully logged-in FritzBox at {Address}", _address);

        var json = await GetData();
        context.State!["internet_connected"] =
            json?["data"]?["internet"]?["connections"]?[0]?["connected"]?.GetValue<bool>();

        _logger.LogInformation("Setup device FritzBox {@DeviceId}", context.ResourceId);
    }

    public async Task OnPullStateAsync(ResourceContext context)
    {
        var json = await GetData();
        context.State!["internet_connected"] =
            json?["data"]?["internet"]?["connections"]?[0]?["connected"]?.GetValue<bool>();

        _logger.LogInformation("Pulled state from FritzBox device {@DeviceId}", context.ResourceId);
    }

    // TODO: Refactor code.

    private async Task<JsonNode?> GetData()
    {
        if (_sid is null) throw new InvalidOperationException("SID cannot be null");

        if (!await IsLoggedIn()) _sid = GetSessionId(_username, _password);

        var parameters = new Dictionary<string, string>
        {
            { "sid", _sid }
        };
        var content = new FormUrlEncodedContent(parameters);
        var response = await _httpClient.PostAsync("data.lua", content);

        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<JsonNode>();
    }

    private string GetSessionId(string username, string password)
    {
        var doc = XDocument.Load($"{_address}/login_sid.lua");
        var sid = GetValue(doc, "SID");
        if (sid != "0000000000000000") return sid;
        var challenge = GetValue(doc, "Challenge");
        var uri = $"{_address}/login_sid.lua?username={username}&response={GetResponse(challenge, password)}";
        doc = XDocument.Load(uri);
        sid = GetValue(doc, "SID");
        return sid;
    }

    private static string GetResponse(string challenge, string password)
    {
        return challenge + "-" + GetMd5Hash(challenge + "-" + password);
    }

    private static string GetMd5Hash(string input)
    {
        var data = MD5.HashData(Encoding.Unicode.GetBytes(input));
        var sb = new StringBuilder();
        foreach (var t in data) sb.Append(t.ToString("x2"));

        return sb.ToString();
    }

    private static string GetValue(XContainer doc, string name)
    {
        var info = doc.FirstNode as XElement;
        return info?.Element(name)?.Value ??
               throw new InvalidOperationException("Could not parse FritzBox information");
    }

    private async Task<bool> IsLoggedIn()
    {
        var content = new StringContent($"xhr=1&sid={_sid}&lang=en&page=overview&xhrId=first&noMenuRef=1");
        var response = await _httpClient.PostAsync("data.lua", content);

        var stringContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode) return false;
        return !stringContent.Contains("\"sid\":\"");
    }
}