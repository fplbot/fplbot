using AlmostServiceBus.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddRedis("redis", port: 6379)
    .WithArgs("--requirepass", "devpassword");
builder.AddServiceBusEmulator("servicebus");

builder.Build().Run();
