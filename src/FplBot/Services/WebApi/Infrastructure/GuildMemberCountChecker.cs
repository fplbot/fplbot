using CronBackgroundServices;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.WebApi.Infrastructure;

// Thin dispatcher: publishes the same RefreshDiscordReachStats an admin "refresh now" click
// publishes, so the cron path and the manual path share one fan-out (see
// RefreshDiscordReachStatsHandler / RefreshGuildMemberCountHandler in EventHandlers). Runs at
// night - low-traffic hours for both fplbot and the Discord API - and offset from Slack's sweep
// so the two don't compete for the bus at the same time.
public class GuildMemberCountChecker(IServiceScopeFactory scopeFactory) : IRecurringAction
{
    public async Task Process(CancellationToken stoppingToken)
    {
        using var activity = FplBotDiagnostics.For(FplBotService.WebApi).StartActivity(nameof(GuildMemberCountChecker));
        using var scope = scopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>().Publish(new RefreshDiscordReachStats(), stoppingToken);
    }

    // 03:00 UTC
    public string Cron => "0 0 3 * * *";
}
