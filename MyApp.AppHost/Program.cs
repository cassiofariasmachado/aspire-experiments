var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var database = builder.AddMongoDB("mongo");

var broker = builder.AddKafka("broker");

var cosmos = builder.AddContainer("cosmos", "mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator")
    .WithHttpsEndpoint(name: "default", containerPort: 8081, hostPort: 8081)
    .WithEnvironment("AZURE_COSMOS_EMULATOR_PARTITION_COUNT", "10")
    .WithEnvironment("AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE", "true");

var api = builder.AddProject<Projects.MyApp_Api>("api")
    .WithReference(cache)
    .WithReference(broker)
    .WithReference(cosmos.GetEndpoint("default"))
    .WithReference(database);

builder.AddProject<Projects.MyApp_Worker>("worker")
    .WithReference(api)
    .WithReference(broker);

builder.Build().Run();