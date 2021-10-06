using CronBackgroundServices;
using Fpl.Workers;
using Fpl.Workers.Abstractions;
using Fpl.Workers.Events;
using Fpl.Workers.Helpers;
using Fpl.Workers.RecurringActions;
using Fpl.Workers.States;
using MediatR;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class FplWorkerServiceCollectionExtensions
    {
        public static IServiceCollection AddFplWorkers(this IServiceCollection services)
        {
            services.AddMediatR(typeof(GameweekMonitoringStarted));
            services.AddSingleton<State>();
            services.AddSingleton<LineupState>();
            services.AddSingleton<DateTimeUtils>();
            services.AddSingleton<IGetMatchDetails, PremierLeagueScraperApi>();
            services.AddSingleton<NearDeadLineMonitor>();
            services.AddSingleton<GameweekLifecycleMonitor>();
            services.AddSingleton<MatchDayStatusMonitor>();
            services.AddRecurrer<GameweekLifecycleRecurringAction>()
                .AddRecurrer<NearDeadlineRecurringAction>()
                .AddRecurrer<MatchDayStatusRecurringAction>();
            return services;
        }
    }
}
