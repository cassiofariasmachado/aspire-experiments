using System.Reflection;
using Confluent.Kafka;
using MassTransit;
using Microsoft.Azure.Cosmos;
using MyApp.ApiService.BackgroundServices;
using MyApp.ApiService.Data.Cosmos;
using MyApp.ApiService.Messaging.Consumers;
using MyApp.ApiService.Messaging.Events;
using MyApp.ApiService.Models;
using MyApp.ApiService.Serializers;
using MyApp.ServiceDefaults;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var (serviceName, serviceVersion) = Assembly.GetExecutingAssembly().GetAssembyNameAndVersion();

// Add service defaults & Aspire components.
builder.AddServiceDefaults(serviceName, serviceVersion);

// Add services to the container.
builder.Services.AddProblemDetails();

var options = new CosmosClientOptions
{
    HttpClientFactory = () => new HttpClient(new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    }),
    ConnectionMode = ConnectionMode.Gateway,
    ServerCertificateCustomValidationCallback = (_, _, _) => true,
    LimitToEndpoint = true
};

// ==> Configure Cosmos connection
var cosmosConnectionString = builder.Configuration.GetConnectionString("Cosmos");

builder.Services.AddSingleton<CosmosClient>(_ =>
    new CosmosClient(cosmosConnectionString, options));

builder.Services.AddScoped<CosmosRepository>();

// ==> Configure Kafka direct connection
builder.AddKafkaProducer<string, Product>("broker",
    settings => { },
    producerSettings => { producerSettings.SetValueSerializer(new JsonSerializer<Product>()); });

// ==> Configure MassTransit
builder.Services.AddMassTransit(x =>
{
    // Injected by Aspire
    var brokerConnectionString = builder.Configuration.GetConnectionString("broker");

    x.UsingInMemory();

    x.AddRider(rider =>
    {
        rider.AddConsumer<MessageReceivedConsumer>();

        rider.AddProducer<MessageReceived>("message-received");

        rider.UsingKafka((context, k) =>
        {
            k.Host(brokerConnectionString);

            k.TopicEndpoint<MessageReceived>("message-received", "consumer-group-message-received",
                e =>
                {
                    e.AutoOffsetReset = AutoOffsetReset.Earliest;
                    e.ConfigureConsumer<MessageReceivedConsumer>(context);
                });
        });
    });
});

// ==> Configure background services
builder.Services.AddHostedService<MessageGenerator>();


var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapGet("/products/{id}", (string id, Tracer tracer) =>
{
    using var span = tracer.StartActiveSpan("[Products API] Get product by id");

    return new Product(id, $"Product {id}", new Random().NextDouble() * 100);
});

app.MapPost("products/produces",
    async (Product product, IProducer<string, Product> producer, CancellationToken cancellationToken) =>
    {
        var message = new Message<string, Product>
        {
            Key = Guid.NewGuid().ToString(),
            Value = product
        };

        await producer.ProduceAsync("products", message, cancellationToken);
    });

app.MapPost("products/cosmos",
    async (Product product, CosmosRepository cosmosRepository, CancellationToken cancellationToken) =>
    {
        await cosmosRepository.SaveProduct(product, cancellationToken);
    });

app.MapGet("products/{id}/cosmos",
    async (string id, CosmosRepository cosmosRepository, CancellationToken cancellationToken) =>
    {
        var product = await cosmosRepository.GetProduct(id, cancellationToken);

        if (product is null)
            return Results.NotFound();

        return Results.Ok(product);
    });

app.MapDefaultEndpoints();

app.Run();