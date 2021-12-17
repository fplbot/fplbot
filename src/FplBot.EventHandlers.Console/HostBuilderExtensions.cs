using FplBot.EventHandlers.Console;
using FplBot.EventHandlers.Discord;
using FplBot.EventHandlers.Slack;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using NServiceBus;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using StackExchange.Redis;
using JsonSerializer = System.Text.Json.JsonSerializer;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Hosting;

public static class HostBuilderExtensions
{
    private const string Discord = "Discord";
    private const string Slack = "Slack";

    public static IHostBuilder CreateDiscordEndpoint(this IHostBuilder builder)
    {
        return builder.CreateCommonEndpointHost(Discord, excludeHandlers: Slack, (c,s) => s.AddDiscordServices(c));
    }

    public static IHostBuilder CreateSlackEndpoint(this IHostBuilder builder)
    {
        return builder.CreateCommonEndpointHost(Slack, excludeHandlers: Discord, (c,s) => s.AddSlackServices(c));
    }

    public static IHostBuilder CreateCommonEndpointHost(this IHostBuilder builder, string chatbot, string excludeHandlers, Action<IConfiguration, IServiceCollection> configureChatbotSpecific)
    {
        return builder.UseMessaging(chatbot, excludeHandlers)
            .ConfigureServices((ctx, services) =>
            {
                services.UseMinimalHttpLogger();
                var redisOptions = HerokuRedisConfigParser.ConfigurationOptions(Environment.GetEnvironmentVariable("REDIS_URL"));
                services.AddStackExchangeRedisCache(o =>
                {
                    o.ConfigurationOptions = redisOptions;
                });
                services.AddFplApiClient(ctx.Configuration);

                var conn = ConnectionMultiplexer.Connect(redisOptions);
                services.AddSingleton<IConnectionMultiplexer>(conn);
                services.AddSingleton<ICaptainsByGameWeek, CaptainsByGameWeek>();
                services.AddSingleton<ITransfersByGameWeek, TransfersByGameWeek>();
                services.AddSingleton<IEntryForGameweek, EntryForGameweek>();
                services.AddSingleton<ILeagueEntriesByGameweek, LeagueEntriesByGameweek>();
                configureChatbotSpecific(ctx.Configuration, services);
            })
            .UseSerilog((hostingContext, loggerConfiguration) =>
                loggerConfiguration
                    .ReadFrom.Configuration(hostingContext.Configuration)
                    .WriteTo.Console(
                        outputTemplate:
                        "[{Level:u3}][{CorrelationId}][{Properties}] {SourceContext} {Message:lj}{NewLine}{Exception}",
                        theme: ConsoleTheme.None));
    }

    private static IHostBuilder UseMessaging(this IHostBuilder host, string chatbot, string exclude)
    {
        host.UseNServiceBus(ctx =>
        {
            if (!ctx.HostingEnvironment.IsDevelopment())
            {
                Console.WriteLine("Using ASB");
                return ctx.AzureServiceBusEndpoint(chatbot, exclude);
            }

            if (ctx.Configuration["ASB_CONNECTIONSTRING"] != null)
            {
                Console.WriteLine($"Using ASB from {Environment.MachineName}");
                return ctx.AzureServiceBusEndpoint(chatbot,exclude, Environment.MachineName);
            }

            Console.WriteLine("Using Learning transport");
            return ctx.LearningTransport(chatbot);
        });
        return host;
    }

    private static EndpointConfiguration AzureServiceBusEndpoint(this HostBuilderContext context, string chatbot, string excludeHandlers, string endpointPostfix = null)
    {
        endpointPostfix = string.IsNullOrEmpty(endpointPostfix) ? string.Empty : $".{endpointPostfix}";
        string endpointName = $"FplBot.EventHandlers.{chatbot}{endpointPostfix}";
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
        var topicName = $"bundle-1{endpointPostfix}";
        transport.TopicName(topicName);
        Console.WriteLine($"Topic: {topicName}");

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

        var scanner = endpointConfiguration.AssemblyScanner();
        string assemblyToExclude = $"FplBot.EventHandlers.{excludeHandlers}.dll";
        Console.WriteLine($"Excluding {assemblyToExclude}");
        scanner.ExcludeAssemblies(assemblyToExclude);

        if (!context.HostingEnvironment.IsDevelopment())
        {
            var metrics = endpointConfiguration.EnableMetrics();
            metrics.SendMetricDataToServiceControl(
                serviceControlMetricsAddress: GetServiceControlMonitoringQueue(context.HostingEnvironment),
                interval: TimeSpan.FromSeconds(30),
                instanceId: $"{endpointName}@{Environment.MachineName}"
            );
        }

        if (!context.HostingEnvironment.IsDevelopment())
        {
            var unique= endpointConfiguration.UniquelyIdentifyRunningInstance();
            if (context.HostingEnvironment.IsProduction())
            {
                unique.UsingCustomIdentifier(Guid.Parse("b93574ee-1cda-4fc5-84a3-23290b2baa41"));
                unique.UsingCustomDisplayName("blank-fplbot/handler");
            }
            else
            {
                unique.UsingCustomIdentifier(Guid.Parse("281aca40-e7a0-46d7-a16c-3a95954ab3a6"));
                unique.UsingCustomDisplayName("blank-fplbot-test/handler");
            }
        }

        endpointConfiguration.CustomDiagnosticsWriter(
            diagnostics =>
            {
                var diagnosticsLite = JsonSerializer.Deserialize<Diagnostics>(diagnostics);
                diagnosticsLite.Chatbot = chatbot;
                Console.WriteLine(diagnosticsLite);
                return Task.CompletedTask;
            });

        return endpointConfiguration;
    }

    record Hosting(string HostId, string HostDisplayName, string MachineName, string HostName, string UserName, string PathToExe);
    record Diagnostics(Hosting Hosting)
    {
        public string Chatbot { get; set; }
    }

    private static string UniqueHostName(string chatbot, IHostEnvironment contextHostingEnvironment)
    {
        if (contextHostingEnvironment.IsDevelopment())
            return Environment.MachineName;

        if (contextHostingEnvironment.IsProduction())
            return $"Heroku.EH";
        return $"Heroku.Test.EH";
    }

    private static string GetServiceControlMonitoringQueue(IHostEnvironment contextHostingEnvironment)
    {
        if (contextHostingEnvironment.IsProduction())
            return "ServiceControl.Monitoring";
        return "ServiceControl.Test.Monitoring";
    }

    private static string GetServiceControlQueue(IHostEnvironment contextHostingEnvironment)
    {
        if (contextHostingEnvironment.IsProduction())
            return "ServiceControl";
        return "ServiceControl.Test";
    }

    private static EndpointConfiguration LearningTransport(this HostBuilderContext context, string chatbot)
    {
        var endpointConfiguration = new EndpointConfiguration($"FplBot.EventHandlers.{chatbot}");
        endpointConfiguration.UseTransport<LearningTransport>();
        endpointConfiguration.UsePersistence<InMemoryPersistence>();
        return endpointConfiguration;
    }
}
