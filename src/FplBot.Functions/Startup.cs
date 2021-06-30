using System;
using FplBot.Functions;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using NServiceBus;
using Slackbot.Net.SlackClients.Http.Extensions;

[assembly: FunctionsStartup(typeof(Startup))]

namespace FplBot.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddSlackClientBuilder();
            var context = builder.GetContext();
            builder.UseNServiceBus(() =>
            {
                string endpointPostfix = "";

                // Workaround for unstable EnvironmentName in Azure
                // (see https://github.com/Azure/azure-functions-host/issues/6239)
                var environmentName = context.Configuration.GetValue<string>("DOTNET_ENVIRONMENT") ??
                                      Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ??
                                      Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ??
                                      context.EnvironmentName;

                if(environmentName == "Development")
                {
                    endpointPostfix += $".{Environment.MachineName}";
                }

                string endpointName = $"FplBot.Functions.{environmentName}{endpointPostfix}";
                var configuration = new ServiceBusTriggeredEndpointConfiguration(endpointName, "ASB_CONNECTIONSTRING");
                var topicName = $"bundle-1{endpointPostfix}";
                configuration.Transport.TopicName(topicName);
                Console.WriteLine($"ENDPOINTNAME: {endpointName}");
                Console.WriteLine($"TOPICNAME: {topicName}");

                configuration.Transport.SubscriptionRuleNamingConvention(t =>
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
                return configuration;
            });
        }
    }
}
