using Refit;

namespace Flownodes.Components.Discord;

public interface IDiscordWebhookApi
{
    Task SendText([Body] DiscordWebhookMessage message);
}