using Fpl.Search;
using FplBot.Data.Slack;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.WebApi.Slack.Abstractions;
using FplBot.WebApi.Slack.Data;
using FplBot.WebApi.Slack.Handlers.SlackEvents;
using FplBot.WebApi.Slack.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nest;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Hosting;
using Slackbot.Net.SlackClients.Http.Extensions;
using StackExchange.Redis;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionFplBotExtensions
{
    public static IServiceCollection AddFplBotSlackWebEndpoints(this IServiceCollection services, IConfiguration config, IConnectionMultiplexer redisConnection)
    {
        services.Configure<SlackRedisOptions>(config);
        services.TryAddSingleton<IConnectionMultiplexer>(redisConnection);
        services.AddSingleton<ISlackTeamRepository, SlackTeamRepository>();
        services.AddFplApiClient(config);
        services.AddSearching(config.GetSection("Search"));
        services.AddSlackClientBuilder();
        services.AddSingleton<ICaptainsByGameWeek, CaptainsByGameWeek>();
        services.AddSingleton<ITransfersByGameWeek, TransfersByGameWeek>();
        services.AddSingleton<IEntryForGameweek, EntryForGameweek>();
        services.AddSingleton<ILeagueEntriesByGameweek, LeagueEntriesByGameweek>();
        services.AddSingleton<IGameweekHelper, GameweekHelper>();
        services.AddSingleton<ISlackWorkSpacePublisher,SlackWorkSpacePublisher>();
        services.AddSingleton<IUninstall, AppUninstaller>();
        services.AddSlackBotEvents<TokenStore>()
            .AddShortcut<HelpEventHandler>()
            .AddAppMentionHandler<FplPlayerCommandHandler>()
            .AddAppMentionHandler<FplStandingsCommandHandler>()
            .AddAppMentionHandler<FplNextGameweekCommandHandler>()
            .AddAppMentionHandler<FplInjuryCommandHandler>()
            .AddAppMentionHandler<FplCaptainCommandHandler>()
            .AddAppMentionHandler<FplTransfersCommandHandler>()
            .AddAppMentionHandler<FplPricesHandler>()
            .AddAppMentionHandler<FplFollowLeagueHandler>()
            .AddAppMentionHandler<FplSubscribeCommandHandler>()
            .AddAppMentionHandler<FplSubscriptionsCommandHandler>()
            .AddAppMentionHandler<DebugHandler>()
            .AddAppMentionHandler<FplSearchHandler>()
            .AddMemberJoinedChannelHandler<FplBotJoinedChannelHandler>()
            .AddInteractiveBlockActionsHandler<InteractiveBlocksActionHandler>()
            .AddAppHomeOpenedHandler<AppHomeOpenedEventHandler>()
            .AddNoOpAppMentionHandler<UnknownAppMentionCommandHandler>();
        return services;
    }
}
