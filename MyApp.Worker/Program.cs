using System.Reflection;
using MyApp.ServiceDefaults;
using MyApp.Worker;
using MyApp.Worker.Clients;
using MyApp.Worker.Clients.Models;
using MyApp.Worker.Serializers;

var builder = Host.CreateApplicationBuilder(args);

var (serviceName, serviceVersion) = Assembly.GetExecutingAssembly().GetAssembyNameAndVersion();

builder.AddServiceDefaults(serviceName, serviceVersion);

builder.AddKafkaConsumer<string, Product>("broker",
    settings => { settings.Config.GroupId = Guid.NewGuid().ToString(); },
    consumerBuilder => { consumerBuilder.SetValueDeserializer(new JsonDeserializer<Product>()); });

builder.Services.AddHttpClient<ProductsApiClient>(client => client.BaseAddress = new("http://api"));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

host.Run();