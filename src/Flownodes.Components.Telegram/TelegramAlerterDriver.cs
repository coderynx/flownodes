using Flownodes.Edge.Core.Alerting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Flownodes.Components.Telegram;

public class TelegramAlerterDriver : IAlerterDriver
{
    private readonly TelegramBotClient _bot;
    private readonly string _chatId;
    private readonly ILogger<TelegramAlerterDriver> _logger;

    public TelegramAlerterDriver(IConfiguration configuration, ILogger<TelegramAlerterDriver> logger)
    {
        _bot = new TelegramBotClient(configuration["TelegramAlerterDriver:Token"]);
        _chatId = configuration["TelegramAlerterDriver:ChatId"];
        _logger = logger;
    }

    public async Task SendAlertAsync(Alert alert)
    {
        var message = "<b>Flownodes</b> \n \n 🚨 New alert \n 📦 Flownodes Resource Name (FRN): " + alert.Frn +
                      " \n 🔖 Alert kind: " + alert.Kind + "\n 💬 Alert message: " + alert.Message;
        await _bot.SendTextMessageAsync(_chatId, message, ParseMode.Html);
        _logger.LogInformation("Sent alert from resource {Frn} to Telegram bot", alert.Frn);
    }
}