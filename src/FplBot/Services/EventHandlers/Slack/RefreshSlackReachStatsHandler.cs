using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack;

// Dispatch only, per CLAUDE.md's fan-out pattern: the per-channel Slack API call happens in
// RefreshChannelMemberCountHandler, one message per subscribed channel. A slightly larger delay
// than Discord's - Slack's conversations.info is more prone to rate-limiting (Tier 3, 50+/min)
// and CI has already hit a live 429 on this workspace/token.
public class RefreshSlackReachStatsHandler(ISlackTeamRepository teamRepo) : IConsumer<RefreshSlackReachStats>
{
    private static readonly TimeSpan DelayBetweenChannels = TimeSpan.FromSeconds(1.5);

    public async Task Consume(ConsumeContext<RefreshSlackReachStats> context)
    {
        foreach (var team in await teamRepo.GetAllInstallations())
        {
            foreach (var channel in team.ChannelSubscriptions)
            {
                await context.Publish(new RefreshChannelMemberCount(team.ExternalId, channel.ChannelId));
                await Task.Delay(DelayBetweenChannels, context.CancellationToken);
            }
        }
    }
}
