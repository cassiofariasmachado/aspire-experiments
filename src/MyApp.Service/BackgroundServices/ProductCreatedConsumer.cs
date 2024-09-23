using System.Text;
using System.Text.Json;
using CloudNative.CloudEvents.Kafka;
using CloudNative.CloudEvents.Protobuf;
using Confluent.Kafka;
using Confluent.Kafka.Extensions.Diagnostics;
using MyApp.Service.Messaging.CloudEvents;
using MyApp.Service.Messaging.Protos;
using OpenTelemetry.Trace;

namespace MyApp.Service.BackgroundServices;

public class ProductCreatedConsumer : BackgroundService
{
    private readonly IConsumer<string, byte[]> _consumer;
    private readonly ILogger<ProductCreatedConsumer> _logger;
    private readonly Tracer _tracer;

    public ProductCreatedConsumer(
        ILogger<ProductCreatedConsumer> logger,
        Tracer tracer,
        IConsumer<string, byte[]> consumer
    )
    {
        _logger = logger;
        _tracer = tracer;
        _consumer = consumer;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => StartConsumingAsync(stoppingToken), stoppingToken);
    }

    private async Task StartConsumingAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe("product-created");

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Factory.StartNew(async () => await ConsumeEventAsync(stoppingToken), stoppingToken);
        }
    }

    private async Task ConsumeEventAsync(CancellationToken stoppingToken)
    {
        try
        {
            _consumer.ConsumeWithInstrumentation(result =>
            {
                if (result is null)
                    return;

                using var span = _tracer.StartActiveSpan("consuming product created", SpanKind.Consumer);

                var cloudEvent = result.Message!.ToCloudEvent(new ProtobufEventFormatter());

                span.SetAttribute("cloudEvent.id", cloudEvent.Id);
                span.SetAttribute("cloudEvent.type", cloudEvent.Type);
                span.SetAttribute("cloudEvent.subject", cloudEvent.Subject);

                var productCreated = cloudEvent.GetMessage<ProductCreated>();

                span.SetAttribute("product.id", productCreated.Id);
                span.SetAttribute("product.name", productCreated.Name);
                span.SetAttribute("product.price", productCreated.Price);

                _logger.LogInformation("consuming event product-created: {message}",
                    JsonSerializer.Serialize(productCreated));
            }, 5000);

            await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
        }
        catch (Exception e)
        {
            _logger.LogError("Exception: {e}", e);
        }
    }
}