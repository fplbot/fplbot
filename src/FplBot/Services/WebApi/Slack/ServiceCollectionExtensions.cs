using Fpl.Search;
using FplBot.ApplicationServices.Slack;
using FplBot.Data.Slack;
using FplBot.Formatting;
using FplBot.Formatting.Helpers;
using FplBot.Integrations.Slack;
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
    public static IServiceCollection AddFplBotSlackWebEndpoints(this IServiceCollection services, IConfiguration config, IConnectionMultiplexer redisConnection,
        IHostEnvironment env)
    {
        services.Configure<RedisOptions>(config);
        services.TryAddSingleton(redisConnection);
        services.AddSingleton<ISlackTeamRepository, SlackTeamRepository>();
        services.AddSingleton<IChannelMemberCountRepository, ChannelMemberCountRepository>();
        services.AddFplApiClient(config);
        services.AddSearching(config.GetSection("Search"));
        services.AddDevAwareSlackClientBuilder(env);
        services.AddSingleton<ICaptainsByGameWeek, CaptainsByGameWeek>();
        services.AddSingleton<ITransfersByGameWeek, TransfersByGameWeek>();
        services.AddSingleton<IEntryForGameweek, EntryForGameweek>();
        services.AddSingleton<ILeagueEntriesByGameweek, LeagueEntriesByGameweek>();
        services.AddScoped<AdminUninstallSlackWorkspace>();
        services.AddSlackBotEvents<SlackbotNetInstallationBridge>()
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
        services.AddTeamContextToAppMentionHandlers();

        return services;
    }

    private static void AddTeamContextToAppMentionHandlers(this IServiceCollection services)
    {
        foreach (var registration in services.Where(d => d.ServiceType == typeof(IHandleAppMentions)).ToList())
        {
            services.Remove(registration);
            services.AddScoped<IHandleAppMentions>(sp => new TeamContextAppMentionHandler(
                (IHandleAppMentions)ActivatorUtilities.CreateInstance(sp, registration.ImplementationType!),
                sp.GetRequiredService<ILogger<TeamContextAppMentionHandler>>()));
        }
    }
}
