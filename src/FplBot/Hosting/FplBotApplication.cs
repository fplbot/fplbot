using System.Net.Security;
using Fpl.Search;
using FplBot.Services.SearchIndexer;
using FplBot.Data.Slack;
using FplBot.EventHandlers.Discord;
using FplBot.EventHandlers.Slack;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.VerifiedEntries;
using FplBot.WebApi.Handlers.Commands;
using FplBot.WebApi.Handlers.Events;
using FplBot.WebApi.Handlers.Sagas;
using FplBot.WebApi.Infrastructure;
using MassTransit;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using StackExchange.Redis;

using DiscordFixtureEventsHandler = FplBot.EventHandlers.Discord.FixtureEventsHandler;
using DiscordFixtureFulltimeHandler = FplBot.EventHandlers.Discord.FixtureFulltimeHandler;
using DiscordGameweekFinishedHandler = FplBot.EventHandlers.Discord.GameweekFinishedHandler;
using DiscordGameweekStartedHandler = FplBot.EventHandlers.Discord.GameweekStartedHandler;
using DiscordInjuryUpdateHandler = FplBot.EventHandlers.Discord.InjuryUpdateHandler;
using DiscordLineupReadyHandler = FplBot.EventHandlers.Discord.LineupReadyHandler;
using DiscordNearDeadlineHandler = FplBot.EventHandlers.Discord.NearDeadlineHandler;
using DiscordNewPlayersHandler = FplBot.EventHandlers.Discord.NewPlayersHandler;
using DiscordPriceChangeHandler = FplBot.EventHandlers.Discord.PriceChangeHandler;
using DiscordFixtureRemovedHandler = FplBot.EventHandlers.Discord.FixtureRemovedFromGameweekHandler;
using SlackFixtureEventsHandler = FplBot.EventHandlers.Slack.FixtureEventsHandler;
using SlackFixtureFulltimeHandler = FplBot.EventHandlers.Slack.FixtureFulltimeHandler;
using SlackGameweekFinishedHandler = FplBot.EventHandlers.Slack.GameweekFinishedHandler;
using SlackGameweekStartedHandler = FplBot.EventHandlers.Slack.GameweekStartedHandler;
using SlackInjuryUpdateHandler = FplBot.EventHandlers.Slack.InjuryUpdateHandler;
using SlackLineupReadyHandler = FplBot.EventHandlers.Slack.LineupReadyHandler;
using SlackNearDeadlineHandler = FplBot.EventHandlers.Slack.NearDeadlineHandler;
using SlackNewPlayerHandler = FplBot.EventHandlers.Slack.NewPlayerHandler;
using SlackPriceChangeHandler = FplBot.EventHandlers.Slack.PriceChangeHandler;
using SlackFixtureRemovedHandler = FplBot.EventHandlers.Slack.FixtureRemovedFromGameweekHandler;

namespace FplBot.Hosting;

public static class FplBotApplication
{
    public static async Task RunAsync(string[] args, IReadOnlyList<FplBotService> activeServices)
    {
        if (activeServices.Contains(FplBotService.WebApi))
            await RunAsWebApplication(args, activeServices);
        else
            await RunAsWorkerHost(args, activeServices);
    }

    private static async Task RunAsWebApplication(string[] args, IReadOnlyList<FplBotService> activeServices)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Host.UseSerilog(ConfigureSerilog);

        var redisConn = BuildRedisConnection(builder.Configuration);
        ConfigureCommon(builder.Services, builder.Configuration, redisConn);
        ConfigureMassTransit(builder.Services, builder.Configuration, activeServices);

        if (activeServices.Contains(FplBotService.WebApi))
            builder.ConfigureWebApp(redisConn);
        if (activeServices.Contains(FplBotService.EventHandlers))
            ConfigureEventHandlers(builder.Services, builder.Configuration, redisConn);
        if (activeServices.Contains(FplBotService.EventPublishers))
            builder.Services.AddFplWorkers();
        if (activeServices.Contains(FplBotService.SearchIndexer))
            ConfigureSearchIndexer(builder.Services, builder.Configuration, redisConn);

        var app = builder.Build();
        app.UseWebApp();
        await app.RunAsync();
    }

    private static async Task RunAsWorkerHost(string[] args, IReadOnlyList<FplBotService> activeServices)
    {
        var host = Host.CreateDefaultBuilder(args)
            .UseSerilog(ConfigureSerilog)
            .ConfigureServices((ctx, services) =>
            {
                var redisConn = BuildRedisConnection(ctx.Configuration);
                ConfigureCommon(services, ctx.Configuration, redisConn);
                ConfigureMassTransit(services, ctx.Configuration, activeServices);

                if (activeServices.Contains(FplBotService.EventHandlers))
                    ConfigureEventHandlers(services, ctx.Configuration, redisConn);
                if (activeServices.Contains(FplBotService.EventPublishers))
                    services.AddFplWorkers();
                if (activeServices.Contains(FplBotService.SearchIndexer))
                    ConfigureSearchIndexer(services, ctx.Configuration, redisConn);
            })
            .Build();

        await host.RunAsync();
    }

    private static void ConfigureSerilog(HostBuilderContext ctx, LoggerConfiguration lc)
    {
        lc.ReadFrom.Configuration(ctx.Configuration)
          .Enrich.WithCorrelationId()
          .Enrich.WithCorrelationIdHeader()
          .WriteTo.Console(
              outputTemplate: "[{Level:u3}][{CorrelationId}][{Properties}] {SourceContext} {Message:lj}{NewLine}{Exception}",
              theme: ConsoleTheme.None);
    }

    private static void ConfigureCommon(IServiceCollection services, IConfiguration config, ConnectionMultiplexer redisConn)
    {
        services.AddSingleton<IConnectionMultiplexer>(redisConn);
        services.AddSingleton(redisConn);
        services.AddStackExchangeRedisCache(o => o.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(redisConn));
        services.AddReducedHttpClientFactoryLogging();
        services.AddFplApiClient(config);
    }

    private static void ConfigureMassTransit(IServiceCollection services, IConfiguration config, IReadOnlyList<FplBotService> activeServices)
    {
        services.AddMassTransit(x =>
        {
            if (activeServices.Contains(FplBotService.WebApi))
            {
                x.AddConsumer<AppInstalledHandler>();
                x.AddConsumer<IndexQueryCommandHandler>();
                x.AddConsumer<GameweekJustBeganUpdateStatsHandler>();
                x.AddConsumer<MatchDayStatusHandler>();
                x.AddConsumer<SeedSelfishnessHandler>();
                x.AddConsumer<AggregatedSuggestionsHandler>();
                x.AddSagaStateMachine<ThrottleEntrySuggestionsSagaStateMachine, AcccumulatedSuggestionsSagaState>()
                    .InMemoryRepository();
                x.AddSagaStateMachine<ThrottlePlSuggestionsSagaStateMachine, AcccumulatedPLSuggestionsSagaState>()
                    .InMemoryRepository();
            }

            if (activeServices.Contains(FplBotService.EventHandlers))
            {
                x.AddConsumer<BroadcastHandler>();
                x.AddConsumer<DiscordFixtureEventsHandler>();
                x.AddConsumer<DiscordFixtureFulltimeHandler>();
                x.AddConsumer<DiscordFixtureRemovedHandler>();
                x.AddConsumer<DiscordGameweekFinishedHandler>();
                x.AddConsumer<DiscordGameweekStartedHandler>();
                x.AddConsumer<DiscordInjuryUpdateHandler>();
                x.AddConsumer<DiscordLineupReadyHandler>();
                x.AddConsumer<DiscordNearDeadlineHandler>();
                x.AddConsumer<DiscordNewPlayersHandler>();
                x.AddConsumer<DiscordPriceChangeHandler>();
                x.AddConsumer<PublishToGuildHandler>();

                x.AddConsumer<SlackFixtureEventsHandler>();
                x.AddConsumer<SlackFixtureFulltimeHandler>();
                x.AddConsumer<SlackFixtureRemovedHandler>();
                x.AddConsumer<SlackGameweekFinishedHandler>();
                x.AddConsumer<SlackGameweekStartedHandler>();
                x.AddConsumer<SlackInjuryUpdateHandler>();
                x.AddConsumer<SlackLineupReadyHandler>();
                x.AddConsumer<SlackNearDeadlineHandler>();
                x.AddConsumer<SlackNewPlayerHandler>();
                x.AddConsumer<SlackPriceChangeHandler>();
                x.AddConsumer<PublishToSlackHandler>();
            }

            x.UsingAzureServiceBus((ctx, cfg) =>
            {
                var connectionString = config.GetConnectionString("servicebus")
                                       ?? config["ASB_CONNECTIONSTRING"]
                                       ?? throw new InvalidOperationException(
                                           "Service bus connection string not configured. Set ConnectionStrings__servicebus or ASB_CONNECTIONSTRING.");
                cfg.Host(connectionString);
                cfg.UseServiceBusMessageScheduler();
                cfg.ConfigureEndpoints(ctx);
            });
        });
    }

    private static void ConfigureEventHandlers(IServiceCollection services, IConfiguration config, ConnectionMultiplexer redisConn)
    {
        services.AddDiscordServices(config);
        services.AddSlackServices(config);
        services.AddSingleton<ICaptainsByGameWeek, CaptainsByGameWeek>();
        services.AddSingleton<ITransfersByGameWeek, TransfersByGameWeek>();
        services.AddSingleton<IEntryForGameweek, EntryForGameweek>();
        services.AddSingleton<ILeagueEntriesByGameweek, LeagueEntriesByGameweek>();
    }

    private static void ConfigureSearchIndexer(IServiceCollection services, IConfiguration config, ConnectionMultiplexer redisConn)
    {
        services.AddVerifiedEntries(config);
        services.AddRecurringIndexer(config, redisConn);
    }

    private static ConnectionMultiplexer BuildRedisConnection(IConfiguration config)
    {
        var connectionString = config.GetConnectionString("redis") ?? config["REDIS_URL"];
        if (connectionString == null)
            throw new InvalidOperationException("Redis connection string not configured. Set ConnectionStrings__redis or REDIS_URL.");

        if (connectionString.StartsWith("redis://", StringComparison.OrdinalIgnoreCase)
            || connectionString.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
        {
            return ConnectionMultiplexer.Connect(ParseRedisUrl(connectionString));
        }

        return ConnectionMultiplexer.Connect(connectionString);
    }

    private static ConfigurationOptions ParseRedisUrl(string redisUrl)
    {
        var uri = new Uri(redisUrl);
        var userInfo = uri.UserInfo.Split(':');
        var host = uri.Host + ":" + uri.Port;
        return new ConfigurationOptions
        {
            ClientName = userInfo[0],
            Password = userInfo.Length > 1 ? userInfo[1] : null,
            EndPoints = { host },
            Ssl = redisUrl.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase),
            SslClientAuthenticationOptions = _ => new SslClientAuthenticationOptions
            {
                TargetHost = uri.Host,
                RemoteCertificateValidationCallback = (_, _, _, _) => true,
            }
        };
    }
}
