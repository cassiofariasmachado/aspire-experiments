using System.Text.Json;
using MassTransit;
using MyApp.ApiService.Messaging.Events;

namespace MyApp.ApiService.Messaging.Consumers;

public class MessageReceivedConsumer : IConsumer<MessageReceived>
{
    private readonly ILogger<MessageReceivedConsumer> _logger;

    public MessageReceivedConsumer(ILogger<MessageReceivedConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<MessageReceived> context)
    {
        _logger.LogInformation("Consuming message received: {message}", JsonSerializer.Serialize(context.Message));

        return Task.CompletedTask;
    }
}