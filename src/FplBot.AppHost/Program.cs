using AlmostServiceBus.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis", port: 6379)
    .WithArgs("--requirepass", "devpassword");
builder.AddServiceBusEmulator("servicebus");

builder.Eventing.Subscribe<ResourceEndpointsAllocatedEvent>(redis.Resource, DevSeeder.SeedAsync);

builder.Build().Run();
