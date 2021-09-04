using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NServiceBus;
using Slackbot.Net.SlackClients.Http.Extensions;

namespace FplBot.Functions
{
    public class Program
    {
        public static void Main()
        {
            IHost host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(s =>
                {
                    s.AddSlackClientBuilder();
                })
                .UseNServiceBus((appConfig,endpointConfig) =>
                {
                    string endpointPostfix = "";

                    // Workaround for unstable EnvironmentName in Azure
                    // (see https://github.com/Azure/azure-functions-host/issues/6239)
                    string environmentName = appConfig.GetValue<string>("DOTNET_ENVIRONMENT") ??
                                             Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ??
                                             Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

                    var connectionString = appConfig.GetValue<string>("AzureWebJobsServiceBus");
                    endpointConfig.Transport.ConnectionString(connectionString);

                    if (environmentName == "Development")
                    {
                        endpointPostfix += $".{Environment.MachineName}";
                    }

                    string topicName = $"bundle-1{endpointPostfix}";
                    endpointConfig.Transport.TopicName(topicName);

                    endpointConfig.Transport.SubscriptionRuleNamingConvention(t =>
                    {
                        return Shorten(t.ToString());

                        static string Shorten(string current)
                        {
                            if (current.Length <= 50)
                            {
                                return current;
                            }

                            string[] strings = current.Split('.');
                            if (strings.Length == 1)
                            {
                                return current.Length > 48 ? $"Z.{current[^48..]}" : $"Z.{current}";
                            }

                            string[] values = strings[1..strings.Length];
                            string newRuleName = string.Join(".", values);
                            return newRuleName.Length > 48 ? Shorten(newRuleName) : $"Z.{newRuleName}";
                        }
                    });
                })
                .Build();

            host.Run();
        }
    }
}
