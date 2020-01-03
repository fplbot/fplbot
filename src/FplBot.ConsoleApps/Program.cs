using Fpl.Client.Clients;
using Fpl.Client.Infra;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Extensions.Publishers.Logger;
using Slackbot.Net.Extensions.Publishers.Slack;

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
                    //services.AddFplApiClient(hostContext.Configuration.GetSection("fpl"));
                    services.AddSlackbotWorker(hostContext.Configuration)
                        .AddPublisher<SlackPublisher>()
                        .AddPublisher<LoggerPublisher>()
                        .AddFplBot(hostContext.Configuration.GetSection("fpl"));
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
