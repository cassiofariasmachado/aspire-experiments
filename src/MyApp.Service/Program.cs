using System.Reflection;
using Confluent.Kafka;
using Confluent.Kafka.Extensions.Diagnostics;
using Confluent.SchemaRegistry;
using Microsoft.Azure.Cosmos;
using MyApp.Service.BackgroundServices;
using MyApp.Service.Data.Cosmos;
using MyApp.Service.Messaging;
using MyApp.Service.Routes;
using MyApp.ServiceDefaults;

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

builder.Services.AddSingleton<ISchemaRegistryClient>(provider => new CachedSchemaRegistryClient(
    new SchemaRegistryConfig()
    {
        Url = builder.Configuration.GetValue<string>("SchemaRegistry:Url")
    })
);

// ==> Configure Kafka
builder.Services.AddSingleton(new ProducerBuilder<string, byte[]>(new ProducerConfig
    {
        BootstrapServers = builder.Configuration.GetConnectionString("broker")
    })
    .BuildWithInstrumentation());

builder.AddKafkaConsumer<string, byte[]>("broker",
    settings => { settings.Config.GroupId = Guid.NewGuid().ToString(); });

// ==> Configure background services
builder.Services.AddHostedService<MessageReceivedGenerator>();
builder.Services.AddHostedService<ProductCreatedConsumer>();

builder.Services.AddScoped<ProtobufProducer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var productsGroup = app.MapGroup("/products").WithOpenApi();

productsGroup.MapGet("/", ProductsRoute.GetAllProducts);
productsGroup.MapGet("{id}", ProductsRoute.GetProduct);
productsGroup.MapPost("/", ProductsRoute.CreateProduct);

app.MapDefaultEndpoints();

app.Run();