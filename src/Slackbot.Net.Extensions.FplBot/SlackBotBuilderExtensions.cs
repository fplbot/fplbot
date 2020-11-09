using System;
using Fpl.Client.Infra;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Hosting;
using Slackbot.Net.Extensions.FplBot;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.GameweekLifecycle;
using Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers;
using Slackbot.Net.Extensions.FplBot.Handlers;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.Extensions.FplBot.RecurringActions;
using Slackbot.Net.SlackClients.Http.Extensions;

// ReSharper disable once CheckNamespace
namespace Slackbot.Net.Abstractions.Hosting
{
    public static class SlackBotBuilderExtensions
    {
        public static IServiceCollection AddDistributedFplBot<T>(this IServiceCollection services, IConfiguration config) where T: class, ISlackTeamRepository
        {
            services.AddReducedHttpClientFactoryLogging();
            services.AddFplApiClient(config);
            services.AddSingleton<ISlackTeamRepository, T>();
            services.AddSlackClientBuilder();
            services.AddSingleton<ICaptainsByGameWeek, CaptainsByGameWeek>();
            services.AddSingleton<ITransfersByGameWeek, TransfersByGameWeek>();
            services.AddSingleton<IGoalsDuringGameweek, GoalsDuringGameweek>();
            services.AddSingleton<IChipsPlayed, ChipsPlayed>();
            services.AddSingleton<IEntryForGameweek, EntryForGameweek>();
            services.AddSingleton<ILeagueEntriesByGameweek, LeagueEntriesByGameweek>();
            services.AddSingleton<IGameweekHelper, GameweekHelper>();
            services.AddSingleton<ISlackWorkSpacePublisher,SlackWorkSpacePublisher>();
            services.AddSingleton<IHandleGameweekStarted, GameweekStartedNotifier>();
            services.AddSingleton<IHandleGameweekEnded, GameweekEndedNotifier>();
            services.AddSingleton<IMonitorState, StateEventsMonitor>();
            services.AddSingleton<FixtureEventsHandler>();
            services.AddSingleton<PriceChangeHandler>();
            services.AddSingleton<StatusUpdateHandler>();
            services.AddSingleton<IState, State>();
            services.AddSingleton<IGameweekMonitorOrchestrator,GameweekMonitorOrchestrator>();
            services.AddSingleton<DateTimeUtils>();
            
            services.AddRecurringActions().AddRecurrer<GameweekLifecycleRecurringAction>()
                .AddRecurrer<NearDeadlineRecurringAction>()
                .Build();
            return services;
        }

        public static IServiceCollection AddFplBotEventHandlers<T>(this IServiceCollection services,
            Action<SlackAppOptions> configuration = null) where T : class, ITokenStore
        {
            services.Configure<SlackAppOptions>(configuration ?? (c => {}));
            services.AddSingleton<IUninstall, AppUninstaller>();
            services.AddSlackBotEvents<T>()
                
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

                .AddMemberJoinedChannelHandler<FplBotJoinedChannelHandler>()
                .AddAppHomeOpenedHandler<AppHomeOpenedEventHandler>();
            return services;
        }
    }

    public class SlackAppOptions
    {
        public string Client_Id { get; set; }
        public string Client_Secret { get; set; }
    }
}