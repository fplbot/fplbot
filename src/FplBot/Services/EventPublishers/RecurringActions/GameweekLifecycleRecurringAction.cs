using CronBackgroundServices;
using Fpl.EventPublishers.Extensions;
using Fpl.EventPublishers.Helpers;
using Fpl.EventPublishers.States;
using FplBot.Hosting;

namespace Fpl.EventPublishers.RecurringActions;

internal class GameweekLifecycleRecurringAction(
    GameweekLifecycleMonitor monitor,
    ILogger<GameweekLifecycleRecurringAction> logger)
    : IRecurringAction
{
    public async Task Process(CancellationToken token)
    {
        using var activity = FplBotDiagnostics.ActivitySource.StartActivity(nameof(GameweekLifecycleRecurringAction));
        using var scope = logger.BeginCorrelationScope();
        logger.LogInformation($"Running {nameof(GameweekLifecycleRecurringAction)}");
        await monitor.EveryOtherMinuteTick(token);
    }

    public string Cron => CronPatterns.EveryOtherMinute;
}
