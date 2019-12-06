using fplbot.consoleapp.Clients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Hosting;
using Slackbot.Net.Publishers;
using System;

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
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHttpClient<IFplClient, FplClient>();
                    services.AddSlackbot(o =>
                        {
                            o.Slackbot_SlackApiKey_SlackApp = "xoxp-10330912275-14635153942-862684278373-678f47af34c54dbd617c2ef5bbfcafca";
                            o.Slackbot_SlackApiKey_BotUser = "xoxb-10330912275-864534450279-wOCTMvNmS5IPDCpeJ2Z8lM7a";
                        })

                        //.AddPublisher<SlackPublisher>()
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
