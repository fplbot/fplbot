using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using CronBackgroundServices;
using Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers;

namespace Slackbot.Net.Extensions.FplBot.RecurringActions
{
    internal class NearDeadlineRecurringAction : IRecurringAction
    {
        private readonly ILogger<NearDeadlineRecurringAction> _logger;
        private readonly NearDeadLineMonitor _nearDeadlineMonitor;

        public NearDeadlineRecurringAction(NearDeadLineMonitor monitor, NearDeadlineHandler handler, ILogger<NearDeadlineRecurringAction> logger)
        {
            _logger = logger;
            _nearDeadlineMonitor = monitor;
            _nearDeadlineMonitor.OneHourToDeadlineHandlers += handler.HandleOneHourToDeadline;
            _nearDeadlineMonitor.TwentyFourHoursToDeadlineHandlers += handler.HandleTwentyFourHoursToDeadline;
        }

        public async Task Process(CancellationToken token)
        {
            _logger.LogInformation($"Running {nameof(NearDeadlineRecurringAction)}");
            using (_logger.BeginCorrelationScope())
            {
                await _nearDeadlineMonitor.EveryMinuteTick();
            }
        }

        public string Cron => Constants.CronPatterns.EveryMinuteAt20Seconds;
    }
}