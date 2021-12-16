using NServiceBus;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Hosting;

public static class HostBuilderExtensions
{
    public static IHostBuilder UseMessaging(this IHostBuilder host)
    {
        host.UseNServiceBus(ctx =>
        {
            if (!ctx.HostingEnvironment.IsDevelopment())
            {
                Console.WriteLine("Using ASB");
                return ctx.AzureServiceBusEndpoint();
            }

            if (ctx.Configuration["ASB_CONNECTIONSTRING"] != null)
            {
                Console.WriteLine($"Using ASB from {Environment.MachineName}");
                return ctx.AzureServiceBusEndpoint(Environment.MachineName);
            }

            Console.WriteLine("Using Learning transport");
            return ctx.LearningTransport();
        });
        return host;
    }

    private static EndpointConfiguration AzureServiceBusEndpoint(this HostBuilderContext context, string endpointPostfix = null)
    {
        endpointPostfix = string.IsNullOrEmpty(endpointPostfix) ? string.Empty : $".{endpointPostfix}";
        string endpointName = $"Fpl.EventPublisher{endpointPostfix}";
        Console.WriteLine($"Endpoint: {endpointName}");
        var endpointConfiguration = new EndpointConfiguration(endpointName);
        endpointConfiguration.SendOnly();
        endpointConfiguration.UseSerialization<NewtonsoftSerializer>();
        endpointConfiguration.License(context.Configuration["NSB_LICENSE"]);
        endpointConfiguration.SendHeartbeatTo(
            serviceControlQueue: GetServiceControlQueue(context.HostingEnvironment),
            frequency: TimeSpan.FromSeconds(15),
            timeToLive: TimeSpan.FromSeconds(30));
        endpointConfiguration.UsePersistence<InMemoryPersistence>();
        var transport = endpointConfiguration.UseTransport<AzureServiceBusTransport>();
        transport.ConnectionString(context.Configuration["ASB_CONNECTIONSTRING"]);
        var topicName = $"bundle-1{endpointPostfix}";
        transport.TopicName(topicName);
        Console.WriteLine($"Topic: {topicName}");

        if (!context.HostingEnvironment.IsDevelopment())
        {
            var unique= endpointConfiguration.UniquelyIdentifyRunningInstance();
            if (context.HostingEnvironment.IsProduction())
            {
                unique.UsingCustomIdentifier(Guid.Parse("fd56af13-62f7-437d-90a8-139a64e4382d"));
                unique.UsingCustomDisplayName("blank-fplbot/publisher");
            }
            else
            {
                unique.UsingCustomIdentifier(Guid.Parse("c2f4bade-4274-46b4-a062-152fbc6229ef"));
                unique.UsingCustomDisplayName("blank-fplbot-test/publisher");
            }
        }
        endpointConfiguration.CustomDiagnosticsWriter(
            diagnostics =>
            {
                var diagnosticsLite = System.Text.Json.JsonSerializer.Deserialize<Diagnostics>(diagnostics);
                Console.WriteLine(diagnosticsLite);
                return Task.CompletedTask;
            });

        return endpointConfiguration;
    }
    record Hosting(string HostId, string HostDisplayName, string MachineName, string HostName, string UserName, string PathToExe);

    record Diagnostics(Hosting Hosting);

    private static string UniqueHostName(IHostEnvironment contextHostingEnvironment)
    {
        if (contextHostingEnvironment.IsDevelopment())
            return Environment.MachineName;

        if (contextHostingEnvironment.IsProduction())
            return $"Heroku.EP";
        return $"Heroku.Test.EP";
    }

    private static string GetServiceControlQueue(IHostEnvironment contextHostingEnvironment)
    {
        if (contextHostingEnvironment.IsProduction())
            return "ServiceControl";
        return "ServiceControl.Test";
    }

    private static EndpointConfiguration LearningTransport(this HostBuilderContext context)
    {
        var endpointConfiguration = new EndpointConfiguration($"Fpl.EventPublisher.{context.HostingEnvironment.EnvironmentName}");
        endpointConfiguration.UseTransport<LearningTransport>();
        endpointConfiguration.UsePersistence<InMemoryPersistence>();
        return endpointConfiguration;
    }
}
