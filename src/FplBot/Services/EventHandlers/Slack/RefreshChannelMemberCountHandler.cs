using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Commands.v1;
using MassTransit;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.EventHandlers.Slack;

// A fetch failure just skips this channel for this sweep - it keeps its last-known (or no) count
// until the next successful sweep. Channel reachability/removal is handled entirely by
// SlackChannelDeliveryFailedHandler's failure-count cleanup, not here.
public class RefreshChannelMemberCountHandler(
    ISlackTeamRepository teamRepo,
    IChannelMemberCountRepository channelMemberCountRepo,
    ISlackClientBuilder slackClientBuilder,
    ILogger<RefreshChannelMemberCountHandler> logger) : IConsumer<RefreshChannelMemberCount>
{
    public async Task Consume(ConsumeContext<RefreshChannelMemberCount> context)
    {
        var (teamId, channelId) = (context.Message.TeamId, context.Message.ChannelId);
        try
        {
            var installation = await teamRepo.FindInstallationByTeamId(teamId);
            if (installation is null)
            {
                return;
            }

            var slackClient = slackClientBuilder.Build(installation.Token);
            var info = await slackClient.ConversationsInfo(channelId, includeNumMembers: true);
            if (info.Channel?.Num_Members is { } count)
            {
                await channelMemberCountRepo.SetMemberCount(channelId, count);
            }
        }
        catch (Exception e)
        {
            logger.LogInformation("MemberCount: {TeamId}/{ChannelId} SKIPPED. Exception: '{ExceptionMessage}'", teamId, channelId, e);
        }
    }
}
