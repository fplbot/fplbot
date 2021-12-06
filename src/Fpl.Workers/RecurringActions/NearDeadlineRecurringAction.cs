using CronBackgroundServices;
using Fpl.Workers.Extensions;
using Fpl.Workers.Helpers;
using Microsoft.Extensions.Logging;

namespace Fpl.Workers.RecurringActions;

internal class NearDeadlineRecurringAction : IRecurringAction
{
    private readonly NearDeadLineMonitor _monitor;
    private readonly ILogger<NearDeadlineRecurringAction> _logger;

    public NearDeadlineRecurringAction(NearDeadLineMonitor monitor, ILogger<NearDeadlineRecurringAction> logger)
    {
        _monitor = monitor;
        _logger = logger;
    }

    public async Task Process(CancellationToken token)
    {
        using (_logger.BeginCorrelationScope())
        {
            _logger.LogInformation($"Running {nameof(NearDeadlineRecurringAction)}");
            await _monitor.EveryMinuteTick();
        }
    }

    public string Cron => CronPatterns.EveryMinuteAt20Seconds;
}
