using Fpl.Client.Infra;
using Fpl.Search;
using Microsoft.Extensions.Options;
using Slackbot.Net.Endpoints.Abstractions;
using Slackbot.Net.Endpoints.Hosting;
using Slackbot.Net.SlackClients.Http.Extensions;
using StackExchange.Redis;
using FplBot.Core;
using FplBot.Core.Abstractions;
using FplBot.Core.Data;
using FplBot.Core.Handlers;
using FplBot.Core.Handlers.SlackEvents;
using FplBot.Core.Helpers;
using MediatR;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionFplBotExtensions
    {
        public static IServiceCollection AddFplBot(this IServiceCollection services, IConfiguration config)
        {
            services.AddFplApiClient(config.GetSection("fpl"));
            services.AddSearching(config.GetSection("Search"));
            services.AddIndexingServices(config.GetSection("Search"));
            services.AddRecurringIndexer(config.GetSection("Search"));
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
            services.AddSingleton<ISlackTeamRepository, RedisSlackTeamRepository>();
            services.AddSlackClientBuilder();
            services.AddSingleton<ICaptainsByGameWeek, CaptainsByGameWeek>();
            services.AddSingleton<ITransfersByGameWeek, TransfersByGameWeek>();
            services.AddSingleton<IGoalsDuringGameweek, GoalsDuringGameweek>();
            services.AddSingleton<IChipsPlayed, ChipsPlayed>();
            services.AddSingleton<IEntryForGameweek, EntryForGameweek>();
            services.AddSingleton<ILeagueEntriesByGameweek, LeagueEntriesByGameweek>();
            services.AddSingleton<IGameweekHelper, GameweekHelper>();
            services.AddSingleton<IVerifiedEntriesService, VerifiedEntriesService>();
            services.AddSingleton<ISlackWorkSpacePublisher,SlackWorkSpacePublisher>();

            services.AddMediatR(typeof(FplEventHandlers));
            services.AddFplWorkers();
            return services;
        }

        public static IServiceCollection AddFplBotSlackEventHandlers(this IServiceCollection services) 
        {            
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