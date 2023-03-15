namespace Flownodes.Components.Discord;

public record DiscordWebhookMessage(string Content, DiscordWebhookEmbed[]? Embeds = null, string[]? Attachments = null);

public record DiscordWebhookEmbed(string Title, string Description);