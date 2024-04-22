using CronBackgroundServices;
using Fpl.EventPublishers;
using Fpl.EventPublishers.Abstractions;
using Fpl.EventPublishers.Events;
using Fpl.EventPublishers.Helpers;
using Fpl.EventPublishers.RecurringActions;
using Fpl.EventPublishers.States;
using MediatR;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class FplWorkerServiceCollectionExtensions
{
    private const string SomeUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.102 Safari/537.36";

    public static IServiceCollection AddFplWorkers(this IServiceCollection services)
    {
        services.AddMediatR(typeof(GameweekMonitoringStarted));
        services.AddSingleton<FixtureState>();
        services.AddSingleton<LineupState>();
        services.AddSingleton<DateTimeUtils>();
        services.AddHttpClient<IGetMatchDetails, PremierLeagueScraperApi>().ConfigureHttpClient(client => client.DefaultRequestHeaders.Add("User-Agent", SomeUserAgent));
        services.AddSingleton<NearDeadLineMonitor>();
        services.AddSingleton<GameweekLifecycleMonitor>();
        services.AddSingleton<MatchDayStatusMonitor>();
        services.AddRecurrer<GameweekLifecycleRecurringAction>()
            .AddRecurrer<NearDeadlineRecurringAction>()
            .AddRecurrer<MatchDayStatusRecurringAction>()
            .AddRecurrer<PlayerUpdatesRecurringAction>();
        return services;
    }
}
