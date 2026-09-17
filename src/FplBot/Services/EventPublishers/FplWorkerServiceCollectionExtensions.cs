using CronBackgroundServices;
using Fpl.EventPublishers.Helpers;
using Fpl.EventPublishers.RecurringActions;
using Fpl.EventPublishers.States;
using Fpl.PulseLive;
using FplBot.Data;
using FplBot.Data.Discord;
using FplBot.Data.Leagues;
using FplBot.Data.Slack;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class FplWorkerServiceCollectionExtensions
{
    private const string SomeUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.102 Safari/537.36";

    public static IServiceCollection AddFplWorkers(this IServiceCollection services, IConfiguration config)
    {
        // Polling for new league entries needs to know which leagues are actually followed, so the
        // publishers need read access to the subscription repositories as well.
        services.Configure<RedisOptions>(config);
        services.TryAddSingleton<ISlackTeamRepository, SlackTeamRepository>();
        services.TryAddSingleton<IGuildRepository, DiscordGuildRepository>();
        services.TryAddSingleton<ILeagueEntriesBookmarkProvider, LeagueEntriesRedisBookmarkProvider>();
        services.AddSingleton<IFixtureState, FixtureState>();
        services.AddSingleton<ILineupState, LineupState>();
        services.AddSingleton<DateTimeUtils>();
        services.AddHttpClient<IPulseLiveClient, PulseLiveClient>().ConfigureHttpClient(client =>
        {
            client.BaseAddress = new Uri("https://sdp-prem-prod.premier-league-prod.pulselive.com");
            client.DefaultRequestHeaders.Add("User-Agent", SomeUserAgent);
            client.DefaultRequestHeaders.Add("Origin", "https://www.premierleague.com");
            client.DefaultRequestHeaders.Add("Referer", "https://www.premierleague.com");
        });
        services.AddSingleton<NearDeadLineMonitor>();
        services.AddSingleton<GameweekLifecycleMonitor>();
        services.AddRecurrer<GameweekLifecycleRecurringAction>()
            .AddRecurrer<NearDeadlineRecurringAction>()
            .AddRecurrer<PlayerUpdatesRecurringAction>()
            .AddRecurrer<NewLeagueEntriesRecurringAction>();
        return services;
    }
}
