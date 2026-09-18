using FplBot.EventHandlers;
using FplBot.EventHandlers.Discord;
using FplBot.EventHandlers.Discord.Commands;
using FplBot.EventHandlers.Slack.Commands;
using FplBot.EventHandlers.Slack;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using Fpl.Search;
using FplBot.Hosting;
using MassTransit;
using StackExchange.Redis;

namespace FplBot.Services.EventHandlers;

public class EventHandlersService : WorkerFplBotService
{
    public override FplBotService ServiceType => FplBotService.EventHandlers;

    public override void Configure(IServiceCollection services, IConfiguration config, ConnectionMultiplexer redis, IHostEnvironment env)
    {
        services.AddDiscordServices(config, env);
        services.AddSlackServices(config, env);
        services.AddSingleton<ICaptainsByGameWeek, CaptainsByGameWeek>();
        services.AddSingleton<ITransfersByGameWeek, TransfersByGameWeek>();
        services.AddSingleton<IEntryForGameweek, EntryForGameweek>();
        services.AddSingleton<ILeagueEntriesByGameweek, LeagueEntriesByGameweek>();
        services.AddSingleton<IGameweekHelper, GameweekHelper>();
        services.AddSearching(config.GetSection("Search"));
    }

    public override void AddConsumers(IBusRegistrationConfigurator cfg)
    {
        cfg.AddConsumer<AppInstalledHandler>();
        cfg.AddConsumer<SlackWorkspaceUninstalledHandler>();
        cfg.AddConsumer<TeamMarkedForRemovalHandler>();
        cfg.AddConsumer<SlackChannelDeliveryFailedHandler>();
        cfg.AddConsumer<DiscordChannelDeliveryFailedHandler>();
        cfg.AddConsumer<ChannelMovedHandler>();
        cfg.AddConsumer<BroadcastHandler>();
        cfg.AddConsumer<DiscordFixtureEventsHandler>();
        cfg.AddConsumer<DiscordFixtureFulltimeHandler>();
        cfg.AddConsumer<DiscordFixtureRemovedHandler>();
        cfg.AddConsumer<DiscordGameweekFinishedHandler>();
        cfg.AddConsumer<DiscordGameweekStartedHandler>();
        cfg.AddConsumer<DiscordInjuryUpdateHandler>();
        cfg.AddConsumer<DiscordLineupReadyHandler>();
        cfg.AddConsumer<DiscordNearDeadlineHandler>();
        cfg.AddConsumer<DiscordNewLeagueEntriesHandler>();
        cfg.AddConsumer<DiscordNewPlayersHandler>();
        cfg.AddConsumer<DiscordPriceChangeHandler>();
        cfg.AddConsumer<PublishToGuildHandler>();
        cfg.AddConsumer<FollowCommandHandler>();
        cfg.AddConsumer<AddSubscriptionCommandHandler>();
        cfg.AddConsumer<RemoveSubscriptionCommandHandler>();
        cfg.AddConsumer<DiscordHelpCommandHandler>();

        cfg.AddConsumer<SlackFixtureEventsHandler>();
        cfg.AddConsumer<SlackFixtureFulltimeHandler>();
        cfg.AddConsumer<SlackFixtureRemovedHandler>();
        cfg.AddConsumer<SlackGameweekFinishedHandler>();
        cfg.AddConsumer<SlackGameweekStartedHandler>();
        cfg.AddConsumer<SlackInjuryUpdateHandler>();
        cfg.AddConsumer<SlackLineupReadyHandler>();
        cfg.AddConsumer<SlackNearDeadlineHandler>();
        cfg.AddConsumer<SlackNewLeagueEntriesHandler>();
        cfg.AddConsumer<SlackNewPlayerHandler>();
        cfg.AddConsumer<SlackPriceChangeHandler>();
        cfg.AddConsumer<PublishToSlackHandler>();
        cfg.AddConsumer<BroadcastToSlackHandler>();

        cfg.AddConsumer<SubscribeCommandHandler>();
        cfg.AddConsumer<SubscriptionsCommandHandler>();
        cfg.AddConsumer<FollowLeagueCommandHandler>();
        cfg.AddConsumer<StandingsCommandHandler>();
        cfg.AddConsumer<CaptainsCommandHandler>();
        cfg.AddConsumer<TransfersCommandHandler>();
        cfg.AddConsumer<InjuriesCommandHandler>();
        cfg.AddConsumer<NextGameweekCommandHandler>();
        cfg.AddConsumer<PlayerCommandHandler>();
        cfg.AddConsumer<PriceChangesCommandHandler>();
        cfg.AddConsumer<SearchCommandHandler>();
        cfg.AddConsumer<DebugCommandHandler>();
        cfg.AddConsumer<HelpCommandHandler>();
        cfg.AddConsumer<UnknownAppMentionCommandHandler>();
        cfg.AddConsumer<BotJoinedChannelHandler>();
    }
}
