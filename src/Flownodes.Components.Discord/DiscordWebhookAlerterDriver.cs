﻿using System.Net.Http.Json;
using Flownodes.Cluster.Core.Alerting;
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

    public Task SendAlertAsync(Alert alert)
    {
        // TODO: Format the message.
        var embed = new DiscordWebhookEmbed("Flownodes alert", alert.Message);
        var message = new DiscordWebhookMessage(alert.Message, new[] { embed });
        _httpClient.PostAsJsonAsync(_url, message);

        return Task.CompletedTask;
    }
}