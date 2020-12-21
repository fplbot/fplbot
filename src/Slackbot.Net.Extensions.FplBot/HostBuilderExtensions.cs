using System;
using NServiceBus;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Hosting
{
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
            var endpointConfiguration = new EndpointConfiguration($"FplBot.{context.HostingEnvironment.EnvironmentName}{endpointPostfix}");
            var persistence = endpointConfiguration.UsePersistence<AzureTablePersistence>();
            persistence.ConnectionString(context.Configuration["AZ_STORAGE_CONNECTIONSTRING"]);
            endpointConfiguration.EnableInstallers();
            endpointConfiguration.License(context.Configuration["NSB_LICENSE"]);
            endpointConfiguration.SendHeartbeatTo(
                serviceControlQueue: GetServiceControlQueue(context.HostingEnvironment),
                frequency: TimeSpan.FromSeconds(15),
                timeToLive: TimeSpan.FromSeconds(30));
            var transport = endpointConfiguration.UseTransport<AzureServiceBusTransport>();
            transport.ConnectionString(context.Configuration["ASB_CONNECTIONSTRING"]);
            transport.RuleNameShortener(r =>
            {
                return Shorten(r);

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
}