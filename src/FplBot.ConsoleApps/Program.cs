using FplBot.ConsoleApps.Clients;
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
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddTransient<IFplClient, FplClient>();
                    services.AddSlackbot(o =>
                        {
                            o.Slackbot_SlackApiKey_SlackApp = "xoxp-10330912275-14635153942-862337698804-1c242dba642c54d3bb46525d90fded60";
                            o.Slackbot_SlackApiKey_BotUser = "xoxb-10330912275-864534450279-WPZRdEtdMsyPFE2ztnWBupQg";
                        })

                        .AddPublisher<SlackPublisher>()
                        //.AddPublisher<LoggerPublisher>()
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
