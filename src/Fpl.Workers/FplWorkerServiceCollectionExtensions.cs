using CronBackgroundServices;
using Fpl.Workers.RecurringActions;
using FplBot.Core;
using FplBot.Core.Abstractions;
using FplBot.Core.GameweekLifecycle;
using FplBot.Core.Helpers;
using FplBot.Core.Models;
using FplBot.Core.RecurringActions;
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
            services.AddHttpClient<IGetMatchDetails, PremierLeagueScraperApi>();
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
