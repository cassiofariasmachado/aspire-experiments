using System.Reflection;
using MyApp.ServiceDefaults;
using MyApp.Worker.BackgroundServices;
using MyApp.Worker.Clients;
using MyApp.Worker.Clients.Models;
using MyApp.Worker.Messaging.Kafka;

var builder = Host.CreateApplicationBuilder(args);

var (serviceName, serviceVersion) = Assembly.GetExecutingAssembly().GetAssembyNameAndVersion();

builder.AddServiceDefaults(serviceName, serviceVersion);

builder.AddKafkaConsumer<string, Product>("broker",
    settings => { settings.Config.GroupId = Guid.NewGuid().ToString(); },
    consumerBuilder => { consumerBuilder.SetValueDeserializer(new JsonDeserializer<Product>()); });

builder.Services.AddHttpClient<ProductsApiClient>(client => client.BaseAddress = new Uri("http://api"));

builder.Services.AddHostedService<VerifyProducts>();

var host = builder.Build();

host.Run();