using FplBot.EventHandlers;
using FplBot.EventHandlers.Discord;
using FplBot.EventHandlers.Slack;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Hosting;
using MassTransit;
using StackExchange.Redis;

namespace FplBot.Services.EventHandlers;

public class EventHandlersService : IFplBotService
{
    public FplBotService ServiceType => FplBotService.EventHandlers;

    public void Configure(IServiceCollection services, IConfiguration config, ConnectionMultiplexer redis, IHostEnvironment env)
    {
        services.AddDiscordServices(config);
        services.AddSlackServices(config);
        services.AddSingleton<ICaptainsByGameWeek, CaptainsByGameWeek>();
        services.AddSingleton<ITransfersByGameWeek, TransfersByGameWeek>();
        services.AddSingleton<IEntryForGameweek, EntryForGameweek>();
        services.AddSingleton<ILeagueEntriesByGameweek, LeagueEntriesByGameweek>();
    }

    public void ConfigureMassTransit(IBusRegistrationConfigurator cfg)
    {
        cfg.AddConsumer<AppInstalledHandler>();
        cfg.AddConsumer<SlackWorkspaceUninstalledHandler>();
        cfg.AddConsumer<BroadcastHandler>();
        cfg.AddConsumer<DiscordFixtureEventsHandler>();
        cfg.AddConsumer<DiscordFixtureFulltimeHandler>();
        cfg.AddConsumer<DiscordFixtureRemovedHandler>();
        cfg.AddConsumer<DiscordGameweekFinishedHandler>();
        cfg.AddConsumer<DiscordGameweekStartedHandler>();
        cfg.AddConsumer<DiscordInjuryUpdateHandler>();
        cfg.AddConsumer<DiscordLineupReadyHandler>();
        cfg.AddConsumer<DiscordNearDeadlineHandler>();
        cfg.AddConsumer<DiscordNewPlayersHandler>();
        cfg.AddConsumer<DiscordPriceChangeHandler>();
        cfg.AddConsumer<PublishToGuildHandler>();

        cfg.AddConsumer<SlackFixtureEventsHandler>();
        cfg.AddConsumer<SlackFixtureFulltimeHandler>();
        cfg.AddConsumer<SlackFixtureRemovedHandler>();
        cfg.AddConsumer<SlackGameweekFinishedHandler>();
        cfg.AddConsumer<SlackGameweekStartedHandler>();
        cfg.AddConsumer<SlackInjuryUpdateHandler>();
        cfg.AddConsumer<SlackLineupReadyHandler>();
        cfg.AddConsumer<SlackNearDeadlineHandler>();
        cfg.AddConsumer<SlackNewPlayerHandler>();
        cfg.AddConsumer<SlackPriceChangeHandler>();
        cfg.AddConsumer<PublishToSlackHandler>();
        cfg.AddConsumer<BroadcastToSlackHandler>();
    }
}
