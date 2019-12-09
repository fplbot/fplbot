using FplBot.ConsoleApps.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Workers.Publishers.Logger;
using Slackbot.Net.Workers.Publishers.Slack;

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
                    c.AddEnvironmentVariables();
                    c.AddJsonFile("appsettings.Local.json", optional: true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<IFplClient, FplClient>();
                    services.Decorate<IFplClient, TryCatchFplClient>();
                    services.AddSlackbotWorker(hostContext.Configuration)
                        .AddPublisher<SlackPublisher>()
                        .AddHandler<FplPlayerCommandHandler>()
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
