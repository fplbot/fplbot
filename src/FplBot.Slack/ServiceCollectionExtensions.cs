using Fpl.Client.Infra;
using Fpl.Search;
using FplBot.Slack.Abstractions;
using FplBot.Slack.Data;
using FplBot.Slack.Data.Abstractions;
using FplBot.Slack.Data.Repositories.Redis;
using FplBot.Slack.Handlers.SlackEvents;
using FplBot.Slack.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Hosting;
using Slackbot.Net.SlackClients.Http.Extensions;
using StackExchange.Redis;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionFplBotExtensions
    {
        public static IServiceCollection AddFplBot(this IServiceCollection services, IConfiguration config)
        {
            services.Configure<RedisOptions>(config);

            services.AddSingleton<ConnectionMultiplexer>(c =>
            {
                var opts = c.GetService<IOptions<RedisOptions>>().Value;
                var options = new ConfigurationOptions
                {
                    ClientName = opts.GetRedisUsername,
                    Password = opts.GetRedisPassword,
                    EndPoints = {opts.GetRedisServerHostAndPort}
                };
                return ConnectionMultiplexer.Connect(options);
            });

            services.AddSingleton<ISlackTeamRepository, SlackTeamRepository>();
            services.AddFplApiClient(config);
            services.AddSearching(config.GetSection("Search"));
            services.AddSlackClientBuilder();
            services.AddSingleton<ICaptainsByGameWeek, CaptainsByGameWeek>();
            services.AddSingleton<ITransfersByGameWeek, TransfersByGameWeek>();
            services.AddSingleton<IChipsPlayed, ChipsPlayed>();
            services.AddSingleton<IEntryForGameweek, EntryForGameweek>();
            services.AddSingleton<ILeagueEntriesByGameweek, LeagueEntriesByGameweek>();
            services.AddSingleton<IGameweekHelper, GameweekHelper>();
            services.AddSingleton<ISlackWorkSpacePublisher,SlackWorkSpacePublisher>();
            services.AddSingleton<IUninstall, AppUninstaller>();
            services.AddSlackBotEvents<SlackTeamRepository>()
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
}
