using System.Text;
using Flownodes.Sdk.Resourcing;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Flownodes.Components.Minecraft;

[BehaviourId("minecraft_lever")]
[BehaviourDescription("A Minecraft lever representation in Flownodes")]
public class MinecraftLever : BaseDevice
{
    private const string Coords = "-78 66 67";
    private readonly ILogger<MinecraftLever> _logger;
    private IModel? _channel;

    private ConnectionFactory? _factory;

    public MinecraftLever(ILogger<MinecraftLever> logger) : base(logger)
    {
        _logger = logger;
    }

    public override Task OnSetupAsync(ResourceContext context)
    {
        _factory = new ConnectionFactory { HostName = "192.168.0.11" };

        using var connection = _factory.CreateConnection();
        _channel = connection.CreateModel();

        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare("TO_FLOWNODES",
                false,
                false,
                false,
                null);

            SetupConsumer(channel);

            var message = $"GET LEVER {Coords}";
            SendMessage(message);

            _logger.LogInformation("Sent lever {LeverCoords} status request", Coords);
        }

        return base.OnSetupAsync(context);
    }

    private void SetupConsumer(IModel channel)
    {
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            _logger.LogInformation("Received lever status: {LeverStatus}", message);
        };
        channel.BasicConsume("TO_FLOWNODES",
            true,
            consumer);
    }

    private void SendMessage(string message)
    {
        var body = Encoding.UTF8.GetBytes(message);
        _channel.BasicPublish("",
            "FROM_FLOWNODES",
            null,
            body);
    }
}