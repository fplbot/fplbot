using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Abstractions.Hosting;
using Slackbot.Net.Configuration;
using Slackbot.Net.Extensions.Publishers.Logger;
using Slackbot.Net.Extensions.Publishers.Slack;
using Slackbot.Net.SlackClients.Http.Extensions;

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
                    services.AddSlackHttpClient(c =>
                    {
                        var slackToken = hostContext.Configuration.GetValue<string>(nameof(SlackOptions.Slackbot_SlackApiKey_BotUser));
                        c.BotToken = slackToken;
                    });
                    services.AddSlackbotWorker(hostContext.Configuration)
                        .AddSlackPublisher()
                        .AddLoggerPublisher()
                        .AddFplBot(hostContext.Configuration.GetSection("fpl"))
                        .BuildRecurrers();
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
