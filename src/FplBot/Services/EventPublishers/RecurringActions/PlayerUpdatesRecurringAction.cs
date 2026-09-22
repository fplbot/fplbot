using CronBackgroundServices;
using Fpl.EventPublishers.Extensions;
using Fpl.EventPublishers.Helpers;
using Fpl.EventPublishers.States;
using FplBot.Hosting;

namespace Fpl.EventPublishers.RecurringActions;

public class PlayerUpdatesRecurringAction(
    PlayerUpdatesMonitor monitor,
    ILogger<PlayerUpdatesRecurringAction> logger)
    : IRecurringAction
{
    public async Task Process(CancellationToken stoppingToken)
    {
        using var activity = FplBotDiagnostics.For(FplBotService.EventPublishers).StartActivity(nameof(PlayerUpdatesRecurringAction));
        using var scope = logger.BeginCorrelationScope();
        await monitor.Tick(stoppingToken);
    }

    public string Cron => CronPatterns.EveryOtherMinuteAt40SecondsSharp;
}
