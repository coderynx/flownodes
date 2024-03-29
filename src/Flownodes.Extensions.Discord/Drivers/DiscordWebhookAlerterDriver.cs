﻿using System.Net.Http.Json;
using Flownodes.Sdk.Alerting;
using Microsoft.Extensions.Configuration;

namespace Flownodes.Extensions.Discord.Drivers;

public class DiscordWebhookAlerterDriver : IAlerterDriver
{
    private readonly HttpClient _httpClient;
    private readonly string _url;

    public DiscordWebhookAlerterDriver(IConfiguration configuration)
    {
        _httpClient = new HttpClient();
        _url = configuration["DiscordAlerterDriver:Url"] ??
               throw new InvalidOperationException("Discord URL not configured");
    }

    public Task SendAlertAsync(AlertToFire alert)
    {
        // TODO: Format the message.
        var embed = new DiscordWebhookEmbed("Flownodes alert", alert.Description);
        var message = new DiscordWebhookMessage(alert.Description, new[] { embed });
        _httpClient.PostAsJsonAsync(_url, message);

        return Task.CompletedTask;
    }

    private record DiscordWebhookMessage(string Content, DiscordWebhookEmbed[]? Embeds = null,
        string[]? Attachments = null);

    private record DiscordWebhookEmbed(string Title, string Description);
}