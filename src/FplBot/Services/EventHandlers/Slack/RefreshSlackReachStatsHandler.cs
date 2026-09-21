using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Services.EventHandlers;
using MassTransit;
using Microsoft.Extensions.Options;

namespace FplBot.EventHandlers.Slack;

// Dispatch only, per CLAUDE.md's fan-out pattern: the per-channel Slack API call happens in
// RefreshChannelMemberCountHandler, one message per subscribed channel. Each channel's command is
// scheduled with an increasing delay via MassTransit's message scheduler, rather than blocking this
// consumer on Task.Delay - a slightly larger step than Discord's, since Slack's conversations.info
// is more prone to rate-limiting (Tier 3, 50+/min) and CI has already hit a live 429 on this
// workspace/token.
public class RefreshSlackReachStatsHandler(ISlackTeamRepository teamRepo, IOptions<ReachStatsSweepOptions> options) : IConsumer<RefreshSlackReachStats>
{
    public async Task Consume(ConsumeContext<RefreshSlackReachStats> context)
    {
        var delayBetweenChannels = options.Value.DelayBetweenSlackChannels;
        var index = 0;
        foreach (var team in await teamRepo.GetAllInstallations())
        {
            foreach (var channel in team.ChannelSubscriptions)
            {
                await context.SchedulePublish(delayBetweenChannels * index, new RefreshChannelMemberCount(team.ExternalId, channel.ChannelId));
                index++;
            }
        }
    }
}
