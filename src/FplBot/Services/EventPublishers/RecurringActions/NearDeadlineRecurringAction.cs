using CronBackgroundServices;
using Fpl.EventPublishers.Extensions;
using Fpl.EventPublishers.Helpers;
using Fpl.EventPublishers.States;
using Microsoft.Extensions.Logging;

namespace Fpl.EventPublishers.RecurringActions;

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
        using var scope = _logger.BeginCorrelationScope();
        using var scope2 = _logger.AddContext("NeardeadlineCheck");
        _logger.LogInformation($"Running {nameof(NearDeadlineRecurringAction)}");
        await _monitor.EveryMinuteTick();
    }

    public string Cron => CronPatterns.EveryMinuteAt20Seconds;
}
