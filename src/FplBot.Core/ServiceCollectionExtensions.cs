using Fpl.Client.Infra;
using Fpl.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Hosting;
using Slackbot.Net.SlackClients.Http.Extensions;
using StackExchange.Redis;
using System;
using FplBot.Core;
using FplBot.Core.Abstractions;
using FplBot.Core.Data;
using FplBot.Core.GameweekLifecycle;
using FplBot.Core.GameweekLifecycle.Handlers;
using FplBot.Core.Handlers;
using FplBot.Core.Helpers;
using FplBot.Core.RecurringActions;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionFplBotExtensions
    {
        public static IServiceCollection AddFplBot(this IServiceCollection services, IConfiguration config)
        {
            services.AddReducedHttpClientFactoryLogging();
            services.AddFplApiClient(config.GetSection("fpl"));
            services.AddSearch(config.GetSection("Search"));
            services.Configure<RedisOptions>(config);
            services.Configure<DistributedSlackAppOptions>(config);
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
            services.AddSingleton<ISlackTeamRepository, RedisSlackTeamRepository>();
            services.AddSlackClientBuilder();
            services.AddSingleton<ICaptainsByGameWeek, CaptainsByGameWeek>();
            services.AddSingleton<ITransfersByGameWeek, TransfersByGameWeek>();
            services.AddSingleton<IGoalsDuringGameweek, GoalsDuringGameweek>();
            services.AddSingleton<IChipsPlayed, ChipsPlayed>();
            services.AddSingleton<IEntryForGameweek, EntryForGameweek>();
            services.AddSingleton<ILeagueEntriesByGameweek, LeagueEntriesByGameweek>();
            services.AddSingleton<IGameweekHelper, GameweekHelper>();
            services.AddSingleton<ISlackWorkSpacePublisher,SlackWorkSpacePublisher>();
            services.AddSingleton<IHandleGameweekStarted, GameweekStartedHandler>();
            services.AddSingleton<IHandleGameweekEnded, GameweekEndedNotifier>();
            services.AddSingleton<IMonitorState, StateEventsMonitor>();
            services.AddSingleton<FixtureEventsHandler>();
            services.AddSingleton<PriceChangeHandler>();
            services.AddSingleton<InjuryUpdateHandler>();
            services.AddSingleton<FixtureFulltimeHandler>();
            services.AddSingleton<IState, State>();
            services.AddSingleton<IGameweekMonitorOrchestrator,GameweekMonitorOrchestrator>();
            services.AddSingleton<DateTimeUtils>();
            services.AddHttpClient<IGetMatchDetails,PremierLeagueScraperApi>();
            services.AddSingleton<MatchState>();
            services.AddSingleton<IMatchStateMonitor, MatchStateMonitor>();
            services.AddSingleton<LineupReadyHandler>();
            services.AddSingleton<NearDeadlineHandler>();
            services.AddSingleton<NearDeadLineMonitor>();
            services.AddRecurringActions().AddRecurrer<GameweekLifecycleRecurringAction>()
                .AddRecurrer<NearDeadlineRecurringAction>()
                .AddRecurrer<IndexerRecurringAction>()
                .Build();
            return services;
        }

        public static IServiceCollection AddFplBotEventHandlers(this IServiceCollection services,
            Action<SlackAppOptions> configuration = null) 
        {
            services.Configure<SlackAppOptions>(configuration ?? (c => {}));
            services.AddSingleton<IUninstall, AppUninstaller>();
            services.AddSlackBotEvents<RedisSlackTeamRepository>()
                
                .AddShortcut<HelpEventHandler>()
                .AddAppMentionHandler<FplPlayerCommandHandler>()
                .AddAppMentionHandler<FplStandingsCommandHandler>()
                .AddAppMentionHandler<FplNextGameweekCommandHandler>()
                .AddAppMentionHandler<FplInjuryCommandHandler>()
                .AddAppMentionHandler<FplCaptainCommandHandler>()
                .AddAppMentionHandler<FplTransfersCommandHandler>()
                .AddAppMentionHandler<FplPricesHandler>()
                .AddAppMentionHandler<FplChangeLeagueIdHandler>()
                .AddAppMentionHandler<FplSubscribeCommandHandler>()
                .AddAppMentionHandler<FplSubscriptionsCommandHandler>()
                .AddAppMentionHandler<DebugHandler>()
                .AddAppMentionHandler<FplSearchHandler>()

                .AddMemberJoinedChannelHandler<FplBotJoinedChannelHandler>()
                .AddInteractiveBlockActionsHandler<InteractiveBlocksActionHandler>()
                .AddAppHomeOpenedHandler<AppHomeOpenedEventHandler>();
            return services;
        }
    }
}