using Refit;

namespace Flownodes.Components.Discord;

public record DiscordWebhookMessage([AliasAs("content")] string Content,
    [AliasAs("embeds")] DiscordWebhookEmbed[]? Embeds = null, [AliasAs("attachments")] string[]? Attachments = null);

public record DiscordWebhookEmbed([AliasAs("title")] string Title, [AliasAs("description")] string Description);