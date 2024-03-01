var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var database = builder.AddMongoDB("mongo");

var broker = builder.AddKafka("broker");

var api = builder.AddProject<Projects.MyApp_Api>("api")
    .WithReference(cache)
    .WithReference(broker)
    .WithReference(database);

builder.AddProject<Projects.MyApp_Worker>("worker")
    .WithReference(api)
    .WithReference(broker);

builder.Build().Run();