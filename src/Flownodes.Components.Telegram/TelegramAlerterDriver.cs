using Flownodes.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Flownodes.Components.Telegram;

[AlerterDriverId("telegram")]
public class TelegramAlerterDriver : IAlerterDriver
{
    private readonly TelegramBotClient _bot;
    private readonly string _chatId;
    private readonly ILogger<TelegramAlerterDriver> _logger;

    public TelegramAlerterDriver(IConfiguration configuration, ILogger<TelegramAlerterDriver> logger)
    {
        _bot = new TelegramBotClient(configuration["TelegramAlerter:Token"]);
        _chatId = configuration["TelegramAlerter:ChatId"];
        _logger = logger;
    }

    public async Task SendAlertAsync(Alert alert)
    {
        var message = "<b>Flownodes</b> \n \n 🚨 New alert \n 📦 Resource Id: " + alert.TargetResourceId +
                      " \n 🔖 Alert kind: " + alert.Severity + "\n 💬 Alert message: " + alert.Description;
        await _bot.SendTextMessageAsync(_chatId, message, ParseMode.Html);
        _logger.LogInformation("Sent alert from resource {TargetResourceId} to Telegram bot", alert.TargetResourceId);
    }
}