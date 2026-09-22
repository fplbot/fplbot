using CronBackgroundServices;
using Fpl.EventPublishers.Helpers;
using Fpl.EventPublishers.RecurringActions;
using Fpl.EventPublishers.States;
using Fpl.PulseLive;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class FplWorkerServiceCollectionExtensions
{
    private const string SomeUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.102 Safari/537.36";

    public static IServiceCollection AddFplWorkers(this IServiceCollection services)
    {
        services.AddSingleton<IFixtureState, FixtureState>();
        services.AddSingleton<ILineupState, LineupState>();
        services.AddSingleton<DateTimeUtils>();
        services.AddPulseLiveClient();
        services.AddSingleton<NearDeadLineMonitor>();
        services.AddSingleton<GameweekLifecycleMonitor>();
        services.AddSingleton<PlayerUpdatesMonitor>();
        services.AddRecurrer<GameweekLifecycleRecurringAction>()
            .AddRecurrer<NearDeadlineRecurringAction>()
            .AddRecurrer<PlayerUpdatesRecurringAction>();
        return services;
    }

    // EventPublishers and WebApi both need this client, and both can be active in the same process
    // (running all four services locally with no --services flag). AddHttpClient doesn't dedupe by
    // itself — calling it twice stacks both ConfigureHttpClient callbacks onto the same named
    // client, adding "Referer" twice and crashing, since it only allows a single value. Guard here
    // so either caller can register it safely regardless of what else is active.
    public static IServiceCollection AddPulseLiveClient(this IServiceCollection services)
    {
        if (services.Any(s => s.ServiceType == typeof(IPulseLiveClient)))
        {
            return services;
        }

        services.AddHttpClient<IPulseLiveClient, PulseLiveClient>().ConfigureHttpClient(client =>
        {
            client.BaseAddress = new Uri("https://sdp-prem-prod.premier-league-prod.pulselive.com");
            client.DefaultRequestHeaders.Add("User-Agent", SomeUserAgent);
            client.DefaultRequestHeaders.Add("Origin", "https://www.premierleague.com");
            client.DefaultRequestHeaders.Add("Referer", "https://www.premierleague.com");
        });
        return services;
    }
}
