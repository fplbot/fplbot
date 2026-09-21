using CronBackgroundServices;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.WebApi.Infrastructure;

// Thin dispatcher: publishes the same RefreshSlackReachStats an admin "refresh now" click
// publishes, so the cron path and the manual path share one fan-out (see
// RefreshSlackReachStatsHandler / RefreshChannelMemberCountHandler in EventHandlers). Runs at
// night, offset 30 minutes from the Discord sweep so the two don't compete for the bus at once.
public class SlackChannelMemberCountChecker(IPublishEndpoint publishEndpoint) : IRecurringAction
{
    public async Task Process(CancellationToken stoppingToken)
    {
        using var activity = FplBotDiagnostics.For(FplBotService.WebApi).StartActivity(nameof(SlackChannelMemberCountChecker));
        await publishEndpoint.Publish(new RefreshSlackReachStats(), stoppingToken);
    }

    // 03:30 UTC
    public string Cron => "0 30 3 * * *";
}
