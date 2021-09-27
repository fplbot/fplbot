using Fpl.Client.Infra;
using Fpl.Search;
using FplBot.Core;
using FplBot.Core.Abstractions;
using FplBot.Core.Handlers;
using FplBot.Core.Handlers.SlackEvents;
using FplBot.Core.Helpers;
using FplBot.Data;
using FplBot.Data.Repositories.Redis;
using MediatR;
using Microsoft.Extensions.Configuration;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Hosting;
using Slackbot.Net.SlackClients.Http.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionFplBotExtensions
    {
        public static IServiceCollection AddFplBot(this IServiceCollection services, IConfiguration config)
        {
            services.AddFplApiClient(config.GetSection("fpl"));
            services.AddRedisData(config);
            services.AddSearching(config.GetSection("Search"));
            services.AddIndexingServices(config.GetSection("Search"));
            services.AddRecurringIndexer(config.GetSection("Search"));

            services.AddSingleton<SelfOwnerShipCalculator>();
            services.AddSlackClientBuilder();
            services.AddSingleton<ICaptainsByGameWeek, CaptainsByGameWeek>();
            services.AddSingleton<ITransfersByGameWeek, TransfersByGameWeek>();
            services.AddSingleton<IChipsPlayed, ChipsPlayed>();
            services.AddSingleton<IEntryForGameweek, EntryForGameweek>();
            services.AddSingleton<ILeagueEntriesByGameweek, LeagueEntriesByGameweek>();
            services.AddSingleton<IGameweekHelper, GameweekHelper>();
            services.AddSingleton<ISlackWorkSpacePublisher,SlackWorkSpacePublisher>();
            return services;
        }

        public static IServiceCollection AddFplBotSlackEventHandlers(this IServiceCollection services)
        {
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

    /// <summary>
    /// Dummy type that should always be in assembly containing public FplBot domain handlers
    /// </summary>
    /// <see cref="DomainEvents.cs"/>
    public class FplEventHandlers {}
}
