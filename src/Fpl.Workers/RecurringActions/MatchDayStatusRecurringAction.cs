using System.Threading;
using System.Threading.Tasks;
using CronBackgroundServices;
using FplBot.Core;
using FplBot.Core.RecurringActions;
using Microsoft.Extensions.Logging;

namespace Fpl.Workers.RecurringActions
{
    internal class MatchDayStatusRecurringAction : IRecurringAction 
    {
        private readonly MatchDayStatusMonitor _monitor;
        private readonly ILogger<GameweekLifecycleMonitor> _logger;

        public MatchDayStatusRecurringAction(MatchDayStatusMonitor monitor, ILogger<GameweekLifecycleMonitor> logger)
        {
            _monitor = monitor;
            _logger = logger;
        }

        public async Task Process(CancellationToken token)
        {            
            using (_logger.BeginCorrelationScope())
            {
                _logger.LogInformation($"Running {nameof(MatchDayStatusRecurringAction)}");
                await _monitor.EveryFiveMinutesTick(token);
            }
        }

        public string Cron => CronPatterns.EveryFiveMinutesAt40seconds;
    }
}
