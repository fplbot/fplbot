using Fpl.Search;
using FplBot.Data.Slack;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Services.WebApi.Slack.Abstractions;
using FplBot.Services.WebApi.Slack.Handlers.Reactors;
using FplBot.Services.WebApi.Slack.Handlers.SlackEvents;
using FplBot.Services.WebApi.Slack.Handlers.SlackEvents.AppMentions;
using FplBot.Services.WebApi.Slack.Helpers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Hosting;
using Slackbot.Net.SlackClients.Http;
using StackExchange.Redis;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionFplBotSlackWebExtensions
{
    public static IServiceCollection AddFplBotSlackWebEndpoints(this IServiceCollection services, IConfiguration config, IConnectionMultiplexer redisConnection, IHostEnvironment env)
    {
        services.Configure<RedisOptions>(config);
        services.TryAddSingleton(redisConnection);
        services.AddSingleton<ISlackTeamRepository, SlackTeamRepository>();
        services.AddFplApiClient(config);
        services.AddSearching(config.GetSection("Search"));
        services.AddDevAwareSlackClientBuilder(env);
        services.AddSingleton<ICaptainsByGameWeek, CaptainsByGameWeek>();
        services.AddSingleton<ITransfersByGameWeek, TransfersByGameWeek>();
        services.AddSingleton<IEntryForGameweek, EntryForGameweek>();
        services.AddSingleton<ILeagueEntriesByGameweek, LeagueEntriesByGameweek>();
        services.AddSingleton<IGameweekHelper, GameweekHelper>();
        services.AddSingleton<ISlackWorkSpacePublisher, SlackWorkSpacePublisher>();
        services.AddSlackBotEvents<TokenManager>()
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
            .AddNoOpAppMentionHandler<UnknownAppMentionCommandHandler>();

        return services;
    }
}
