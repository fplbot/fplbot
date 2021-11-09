using CronBackgroundServices;
using Fpl.Workers.Extensions;
using Fpl.Workers.Helpers;
using Microsoft.Extensions.Logging;

namespace Fpl.Workers.RecurringActions;

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