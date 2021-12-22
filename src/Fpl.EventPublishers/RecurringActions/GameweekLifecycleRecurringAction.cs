using CronBackgroundServices;
using Fpl.EventPublishers.Extensions;
using Fpl.EventPublishers.Helpers;
using Fpl.EventPublishers.States;
using Microsoft.Extensions.Logging;

namespace Fpl.EventPublishers.RecurringActions;

internal class GameweekLifecycleRecurringAction : IRecurringAction
{
    private readonly GameweekLifecycleMonitor _monitor;
    private readonly ILogger<GameweekLifecycleRecurringAction> _logger;

    public GameweekLifecycleRecurringAction(GameweekLifecycleMonitor monitor, ILogger<GameweekLifecycleRecurringAction> logger)
    {
        _monitor = monitor;
        _logger = logger;
    }

    public async Task Process(CancellationToken token)
    {
        using var scope = _logger.BeginCorrelationScope();
        _logger.LogInformation($"Running {nameof(GameweekLifecycleRecurringAction)}");
        await _monitor.EveryOtherMinuteTick(token);
    }

    public string Cron => CronPatterns.EveryOtherMinute;
}
