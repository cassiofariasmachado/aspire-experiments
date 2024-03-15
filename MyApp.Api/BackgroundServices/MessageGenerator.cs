using System.Text.Json;
using MassTransit;
using MyApp.ApiService.Messaging.Events;

namespace MyApp.ApiService.BackgroundServices;

public class MessageGenerator : BackgroundService
{
    private readonly ILogger<MessageGenerator> _logger;
    private readonly IServiceProvider _serviceProvider;
    private ITopicProducer<MessageReceived> producer;

    public MessageGenerator(ILogger<MessageGenerator> logger, IServiceProvider serviceProvider)
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
        using (var scope = _serviceProvider.CreateScope())
        {
            PopulateServices(scope);

            var message = new MessageReceived
            {
                Text = "Hello MassTransit",
                ReceivedAt = DateTime.UtcNow
            };

            await producer.Produce(message, stoppingToken);

            _logger.LogInformation("Producing message received: {message}", JsonSerializer.Serialize(message));

            await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
        }
    }

    private void PopulateServices(IServiceScope scope)
    {
        producer = scope.ServiceProvider.GetRequiredService<ITopicProducer<MessageReceived>>();
    }
}