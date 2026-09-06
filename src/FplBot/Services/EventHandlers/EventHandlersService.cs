using FplBot.EventHandlers;
using FplBot.EventHandlers.Discord;
using FplBot.EventHandlers.Slack;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Hosting;
using MassTransit;
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
        cfg.AddConsumer<IndexQueryCommandHandler>();
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
    }
}
