using System.Reflection;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using MyApp.ApiService.Models;
using MyApp.ServiceDefaults;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

var (serviceName, serviceVersion) = Assembly.GetExecutingAssembly().GetAssembyNameAndVersion();

// Add service defaults & Aspire components.
builder.AddServiceDefaults(serviceName, serviceVersion);

// Add services to the container.
builder.Services.AddProblemDetails();


builder.AddKafkaProducer<string, Product>("broker",
    (settings) => { },
    producerSettings => { producerSettings.SetValueSerializer(new JsonSerializer<Product>()); });

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

app.MapDefaultEndpoints();

app.Run();

public class JsonSerializer<T> : ISerializer<T>
{
    public byte[] Serialize(T data, SerializationContext context)
    {
        return JsonSerializer.SerializeToUtf8Bytes(data);
    }
}