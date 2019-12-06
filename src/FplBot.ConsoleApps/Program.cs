using System;
using FplBot.ConsoleApps.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Hosting;
using Slackbot.Net.Publishers;
using Slackbot.Net.Publishers.Slack;

namespace FplBot.ConsoleApps
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(c =>
                {
                    c.AddJsonFile("appsettings.Local.json", optional: true);
                    c.AddEnvironmentVariables();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<IFplClient, FplClient>();
                    services.AddSlackbot(hostContext.Configuration)

                        .AddPublisher<SlackPublisher>()
                        .AddPublisher<LoggerPublisher>()
                        .AddHandler<FplCommandHandler>();

                })
                .ConfigureLogging((context, configLogging) =>
                {
                    configLogging
                        .SetMinimumLevel(LogLevel.Trace)
                        .AddConsole(c => c.DisableColors = true)
                        .AddDebug();
                });
    }
}
