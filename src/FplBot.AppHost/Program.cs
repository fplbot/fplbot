using AlmostServiceBus.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis", port: 6379)
    .WithArgs("--requirepass", "devpassword");
builder.AddServiceBusEmulator("servicebus");
var elasticsearch = builder.AddElasticsearch("elasticsearch",
        password: builder.AddParameter("elasticsearch-password", "dev", secret: true), port: 9200);

builder.Eventing.Subscribe<ResourceEndpointsAllocatedEvent>(redis.Resource, DevSeeder.SeedAsync);
builder.Eventing.Subscribe<ResourceEndpointsAllocatedEvent>(elasticsearch.Resource, DevSeeder.SeedElasticsearchAsync);

builder.Build().Run();
