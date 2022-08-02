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
    public static IServiceCollection AddFplWorkers(this IServiceCollection services)
    {
        services.AddMediatR(typeof(GameweekMonitoringStarted));
        services.AddSingleton<State>();
        services.AddSingleton<LineupState>();
        services.AddSingleton<DateTimeUtils>();
        services.AddHttpClient<IGetMatchDetails, PremierLeagueScraperApi>();
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
