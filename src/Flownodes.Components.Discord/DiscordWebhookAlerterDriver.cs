using System.Net.Http.Json;
using Flownodes.Sdk.Alerting;
using Microsoft.Extensions.Configuration;

namespace Flownodes.Components.Discord;

public class DiscordWebhookAlerterDriver : IAlerterDriver
{
    private readonly HttpClient _httpClient;
    private readonly string _url;

    public DiscordWebhookAlerterDriver(IConfiguration configuration)
    {
        _httpClient = new HttpClient();
        _url = configuration["DiscordAlerterDriver:Url"];
    }

    public Task SendAlertAsync(AlertToFire alert)
    {
        // TODO: Format the message.
        var embed = new DiscordWebhookEmbed("Flownodes alert", alert.Description);
        var message = new DiscordWebhookMessage(alert.Description, new[] { embed });
        _httpClient.PostAsJsonAsync(_url, message);

        return Task.CompletedTask;
    }
}