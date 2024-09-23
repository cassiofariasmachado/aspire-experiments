using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using MyApp.Service.Messaging;
using MyApp.Service.Messaging.Protos;

namespace MyApp.Service.BackgroundServices;

public class MessageReceivedGenerator : BackgroundService
{
    private readonly ILogger<MessageReceivedGenerator> _logger;
    private readonly IServiceProvider _serviceProvider;

    private ProtobufProducer _producer;

    public MessageReceivedGenerator(ILogger<MessageReceivedGenerator> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
            await SendMessageAsync(stoppingToken);
    }

    private async Task SendMessageAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();

        PopulateServices(scope);

        var topic = "message-received";
        var partitionKey = Guid.NewGuid().ToString();

        var messageReveived = new MessageReveived
        {
            Text = "Hello",
            ReceivedAt = DateTime.UtcNow.ToTimestamp()
        };

        await _producer.ProduceAsync(topic, messageReveived, partitionKey, stoppingToken);

        _logger.LogInformation("consuming event message-received: {message}",
            JsonSerializer.Serialize(messageReveived));

        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
    }

    private void PopulateServices(IServiceScope scope)
    {
        _producer = scope.ServiceProvider.GetRequiredService<ProtobufProducer>();
    }
}