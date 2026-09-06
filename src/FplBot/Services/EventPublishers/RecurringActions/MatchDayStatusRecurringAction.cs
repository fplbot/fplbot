using CronBackgroundServices;
using Fpl.EventPublishers.Extensions;
using Fpl.EventPublishers.Helpers;
using Fpl.EventPublishers.States;

namespace Fpl.EventPublishers.RecurringActions;

internal class MatchDayStatusRecurringAction(
    MatchDayStatusMonitor monitor,
    ILogger<MatchDayStatusRecurringAction> logger)
    : IRecurringAction
{
    public async Task Process(CancellationToken token)
    {
        using var scope = logger.BeginCorrelationScope();
        using var scope2 = logger.AddContext("MatchdaystatusCheck");
        logger.LogInformation($"Running {nameof(MatchDayStatusRecurringAction)}");
        await monitor.EveryFiveMinutesTick(token);
    }

    public string Cron => CronPatterns.EveryFiveMinutesAt40seconds;
}
