using CronBackgroundServices;
using Fpl.EventPublishers.Extensions;
using Fpl.EventPublishers.Helpers;
using Fpl.EventPublishers.States;

namespace Fpl.EventPublishers.RecurringActions;

internal class NearDeadlineRecurringAction(NearDeadLineMonitor monitor, ILogger<NearDeadlineRecurringAction> logger)
    : IRecurringAction
{
    public async Task Process(CancellationToken token)
    {
        using var scope = logger.BeginCorrelationScope();
        using var scope2 = logger.AddContext("NeardeadlineCheck");
        logger.LogInformation($"Running {nameof(NearDeadlineRecurringAction)}");
        await monitor.EveryMinuteTick();
    }

    public string Cron => CronPatterns.EveryMinuteAt20Seconds;
}
