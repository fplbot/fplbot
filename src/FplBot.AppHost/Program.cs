using AlmostServiceBus.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");
var serviceBus = builder.AddServiceBusEmulator("servicebus");

builder.AddProject<Projects.FplBot>("webapi")
    .WithArgs("--services", "WebApi")
    .WithReference(redis)
    .WithReference(serviceBus);

builder.AddProject<Projects.FplBot>("eventhandlers")
    .WithArgs("--services", "EventHandlers")
    .WithReference(redis)
    .WithReference(serviceBus);

builder.AddProject<Projects.FplBot>("eventpublishers")
    .WithArgs("--services", "EventPublishers")
    .WithReference(redis)
    .WithReference(serviceBus);

builder.Build().Run();
