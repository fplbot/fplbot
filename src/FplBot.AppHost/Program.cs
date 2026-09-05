using AlmostServiceBus.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddRedis("redis");
builder.AddServiceBusEmulator("servicebus");

builder.Build().Run();
