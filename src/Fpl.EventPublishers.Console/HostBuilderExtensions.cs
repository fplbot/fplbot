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
        string endpointName = $"Fpl.EventPublisher";
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

        return endpointConfiguration;
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
