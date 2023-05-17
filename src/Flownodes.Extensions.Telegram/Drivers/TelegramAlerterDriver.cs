using Flownodes.Sdk.Alerting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Flownodes.Extensions.Telegram.Drivers;

[AlerterDriverId("telegram")]
public class TelegramAlerterDriver : IAlerterDriver
{
    private readonly TelegramBotClient _bot;
    private readonly string _chatId;
    private readonly ILogger<TelegramAlerterDriver> _logger;

    public TelegramAlerterDriver(IConfiguration configuration, ILogger<TelegramAlerterDriver> logger)
    {
        var token = configuration["Telegram:Token"] ?? throw new NullReferenceException("Token cannot be null");
        _chatId = configuration["Telegram:ChatId"] ?? throw new Exception("Chat is cannot be null");
        _bot = new TelegramBotClient(token);
        _logger = logger;
    }

    public async Task SendAlertAsync(AlertToFire alert)
    {
        var message = "<b>Flownodes</b> \n \n 🚨 New alert \n 📦 Resource Id: " + alert.TargetResourceId +
                      " \n 🔖 Alert kind: " + alert.Severity + "\n 💬 Alert message: " + alert.Description;
        await _bot.SendTextMessageAsync(_chatId, message, parseMode: ParseMode.Html);
        _logger.LogInformation("Sent alert from resource {TargetResourceId} to Telegram bot", alert.TargetResourceId);
    }
}