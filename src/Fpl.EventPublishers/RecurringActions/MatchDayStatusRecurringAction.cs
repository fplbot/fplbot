using CronBackgroundServices;
using Fpl.EventPublishers.Extensions;
using Fpl.EventPublishers.Helpers;
using Fpl.EventPublishers.States;
using Microsoft.Extensions.Logging;

namespace Fpl.EventPublishers.RecurringActions;

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
