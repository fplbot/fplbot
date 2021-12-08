using NServiceBus;
using NServiceBus.Features;

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
        string endpointName = $"FplBot.{context.HostingEnvironment.EnvironmentName}{endpointPostfix}";
        Console.WriteLine($"Endpoint: {endpointName}");
        var endpointConfiguration = new EndpointConfiguration(endpointName);
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

        transport.SubscriptionRuleNamingConvention((Type t) =>
        {
            return Shorten(t.ToString());

            static string Shorten(string current)
            {
                if (current.Length <= 50)
                    return current;

                var strings = current.Split('.');
                if (strings.Length == 1)
                {
                    return current.Length > 48 ? $"Z.{current[^48..]}" : $"Z.{current}";
                }

                var values = strings[1..(strings.Length)];
                var newRuleName = string.Join(".", values);
                return newRuleName.Length > 48 ? Shorten(newRuleName) : $"Z.{newRuleName}";
            }
        });
        var recoverabilty = endpointConfiguration.Recoverability();
        recoverabilty.Immediate(s => s.NumberOfRetries(0));
        recoverabilty.Delayed(s => s.NumberOfRetries(3).TimeIncrease(TimeSpan.FromSeconds(20)));

        // ℹ️ Disabled in dev to decrease webapp startup time
        //
        // Enable when you want to create new queues (i.e. new developer and/or endpoint) without
        // using the `asb-transport` CLI tool
        if (!context.HostingEnvironment.IsDevelopment())
        {
            endpointConfiguration.EnableInstallers();
        }

        // ℹ️ Disabled in dev To decrease web app startup time
        //
        // Enable interim if you want to test _new_ events and/or commands
        if(context.HostingEnvironment.IsDevelopment())
        {
            endpointConfiguration.DisableFeature<AutoSubscribe>();
        }

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
        var endpointConfiguration = new EndpointConfiguration($"FplBot.{context.HostingEnvironment.EnvironmentName}");
        endpointConfiguration.UseTransport<LearningTransport>();
        endpointConfiguration.UsePersistence<InMemoryPersistence>();
        return endpointConfiguration;
    }
}
