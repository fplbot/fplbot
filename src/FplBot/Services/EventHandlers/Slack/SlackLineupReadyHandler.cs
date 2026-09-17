using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.SlackClients.Http.Models.Requests.ChatPostMessage;

namespace FplBot.EventHandlers.Slack;

public class SlackLineupReadyHandler(
    ISlackTeamRepository slackTeamRepo,
    ISlackWorkSpacePublisher publisher,
    ILogger<SlackLineupReadyHandler> logger)
    : IConsumer<LineupReady>, IConsumer<PublishLineupsToSlackWorkspace>
{
    public async Task Consume(ConsumeContext<LineupReady> context)
    {
        var message = context.Message;
        logger.LogInformation("Handling new lineups");
        var subscribedChannels = await slackTeamRepo.GetChannelsSubscribedTo(FplEvent.Lineups);

        foreach (var (teamId, channelId) in subscribedChannels)
        {
            await context.Publish(new PublishLineupsToSlackWorkspace(teamId, channelId, message.Lineup));
        }
    }

    public async Task Consume(ConsumeContext<PublishLineupsToSlackWorkspace> context)
    {
        var message = context.Message;
        var channelId = message.ChannelId;
        var lineups = message.Lineups;
        var firstMessage = $"*Lineups {lineups.HomeTeamLineup.TeamName}-{lineups.AwayTeamLineup.TeamName} ready* 👇";

        var res = await publisher.PublishToWorkspaceWithResponse(message.WorkspaceId,
            new ChatPostMessageRequest { Channel = channelId, Text = firstMessage });
        if (res is not null)
        {
            var formattedLineup = Formatter.FormatLineup(lineups);
            await context.Publish(new PublishSlackThreadMessage
            (
                message.WorkspaceId,
                channelId,
                res.ts,
                formattedLineup
            ));
        }
    }
}
