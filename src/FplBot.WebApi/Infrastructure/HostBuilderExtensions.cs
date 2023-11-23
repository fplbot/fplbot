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
                return ctx.AzureServiceBusEndpoint();
            }

            Console.WriteLine("Using Learning transport");
            return ctx.LearningTransport();
        });
        return host;
    }

    private static EndpointConfiguration AzureServiceBusEndpoint(this HostBuilderContext context)
    {

        var endpointName = $"FplBot.WebApi";
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

        if(context.HostingEnvironment.IsDevelopment()){
            transport.TopicName(Environment.MachineName);
            Console.WriteLine($"Using non-default topic: {Environment.MachineName}");
        }

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

        endpointConfiguration.EnableInstallers();


        if (!context.HostingEnvironment.IsDevelopment())
        {
            var unique= endpointConfiguration.UniquelyIdentifyRunningInstance();
            if (context.HostingEnvironment.IsProduction())
            {
                unique.UsingCustomIdentifier(Guid.Parse("c6d14bec-0da5-4403-b0ec-c3f3b738c11d"));
                unique.UsingCustomDisplayName("blank-fplbot/web");
            }
            else
            {
                unique.UsingCustomIdentifier(Guid.Parse("d8bcefac-f469-4daa-a94f-807c5b959d78"));
                unique.UsingCustomDisplayName("blank-fplbot-test/web");
            }
        }

        endpointConfiguration.CustomDiagnosticsWriter(
            diagnostics =>
            {
                var diagnosticsLite = System.Text.Json.JsonSerializer.Deserialize<Diagnostics>(diagnostics);
                Console.WriteLine(diagnosticsLite.Hosting);
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
            return $"Heroku.Web";
        return $"Heroku.Test.Web";
    }

    private static string GetServiceControlQueue(IHostEnvironment contextHostingEnvironment)
    {
        if (contextHostingEnvironment.IsProduction())
            return "ServiceControl";
        return "ServiceControl.Test";
    }

    private static EndpointConfiguration LearningTransport(this HostBuilderContext context)
    {
        var endpointConfiguration = new EndpointConfiguration($"FplBot.WebApi");
        endpointConfiguration.UseTransport<LearningTransport>();
        endpointConfiguration.UsePersistence<InMemoryPersistence>();
        return endpointConfiguration;
    }
}
