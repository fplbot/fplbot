using System.Threading;
using System.Threading.Tasks;
using CronBackgroundServices;
using Fpl.Workers;
using Microsoft.Extensions.Logging;

namespace FplBot.Core.RecurringActions
{
    internal class GameweekLifecycleRecurringAction : IRecurringAction 
    {
        private readonly GameweekLifecycleMonitor _monitor;
        private readonly ILogger<GameweekLifecycleMonitor> _logger;

        public GameweekLifecycleRecurringAction(GameweekLifecycleMonitor monitor, ILogger<GameweekLifecycleMonitor> logger)
        {
            _monitor = monitor;
            _logger = logger;
        }

        public async Task Process(CancellationToken token)
        {            
            using (_logger.BeginCorrelationScope())
            {
                _logger.LogInformation($"Running {nameof(GameweekLifecycleRecurringAction)}");
                await _monitor.EveryOtherMinuteTick(token);
            }
        }

        public string Cron => CronPatterns.EveryOtherMinute;
    }
}
