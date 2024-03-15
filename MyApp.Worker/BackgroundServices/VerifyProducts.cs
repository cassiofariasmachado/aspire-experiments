using Confluent.Kafka;
using MyApp.Worker.Clients;
using MyApp.Worker.Clients.Models;
using OpenTelemetry.Trace;

namespace MyApp.Worker.BackgroundServices;

public class VerifyProducts : BackgroundService
{
    private readonly IConsumer<string, Product> _consumer;
    private readonly ILogger<VerifyProducts> _logger;
    private readonly ProductsApiClient _productsApiClient;
    private readonly Tracer _tracer;

    public VerifyProducts(ILogger<VerifyProducts> logger, ProductsApiClient productsApiClient, Tracer tracer,
        IConsumer<string, Product> consumer)
    {
        _logger = logger;
        _productsApiClient = productsApiClient;
        _tracer = tracer;
        _consumer = consumer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
            await VerifyProductsAsync(stoppingToken);
    }

    private async Task VerifyProductsAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var span = _tracer.StartActiveSpan("[Products Worker] Verifying products");

            _consumer.Subscribe("products");

            var consumeResult = _consumer.Consume(stoppingToken);

            if (consumeResult is null)
            {
                _logger.LogInformation("No products.");
                return;
            }

            var product = await _productsApiClient.GetProductAsync(consumeResult.Message.Value.Id);

            if (product is not null)
            {
                span.SetAttribute("product.id", product.Id);
                span.SetAttribute("product.name", product.Name);
                span.SetAttribute("product.price", product.Price);
            }

            if (_logger.IsEnabled(LogLevel.Information))
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
        }
        catch (Exception e)
        {
            _logger.LogError("Exception: {e}", e);
        }
    }
}