using AlmostServiceBus.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var redisPassword = builder.AddParameter("redis-password", "devpassword", secret: true);
var redis = builder.AddRedis("redis", port: 6379, password: redisPassword);
builder.AddServiceBusEmulator("servicebus", dashboardPort:20000, port: 6000);
var elasticsearch = builder.AddElasticsearch("elasticsearch",
        password: builder.AddParameter("elasticsearch-password", "dev", secret: true), port: 9200)
    .WithEndpoint("internal", e => e.Port = 9300);

builder.Eventing.Subscribe<ResourceEndpointsAllocatedEvent>(redis.Resource, DevSeeder.SeedAsync);
builder.Eventing.Subscribe<ResourceEndpointsAllocatedEvent>(elasticsearch.Resource, DevSeeder.SeedElasticsearchAsync);

builder.Build().Run();
