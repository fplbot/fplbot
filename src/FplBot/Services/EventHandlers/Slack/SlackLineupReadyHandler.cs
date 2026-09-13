using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.EventHandlers.Slack;

public class SlackLineupReadyHandler(
    ISlackTeamRepository slackTeamRepo,
    ISlackClientBuilder builder,
    ILogger<SlackLineupReadyHandler> logger)
    : IConsumer<LineupReady>, IConsumer<PublishLineupsToSlackWorkspace>
{
    public async Task Consume(ConsumeContext<LineupReady> context)
    {
        var message = context.Message;
        logger.LogInformation("Handling new lineups");
        var installations = await slackTeamRepo.GetAllInstallations();

        foreach (var installation in installations)
        {
            if (installation.HasRegisteredFor(FplEvent.Lineups))
            {
                await context.Publish(new PublishLineupsToSlackWorkspace(installation.TeamId, message.Lineup));
            }
        }
    }

    public async Task Consume(ConsumeContext<PublishLineupsToSlackWorkspace> context)
    {
        var message = context.Message;
        var installation = await slackTeamRepo.GetInstallation(message.WorkspaceId);
        var channelId = installation.PrimaryChannel()?.ChannelId;
        var slackClient = builder.Build(installation.Token);
        var lineups = message.Lineups;
        var firstMessage = $"*Lineups {lineups.HomeTeamLineup.TeamName}-{lineups.AwayTeamLineup.TeamName} ready* 👇";

        var res = await slackClient.ChatPostMessage(channelId, firstMessage);
        if (res.Ok)
        {
            var formattedLineup = Formatter.FormatLineup(lineups);
            await context.Publish(new PublishSlackThreadMessage
            (
                message.WorkspaceId,
                channelId!,
                res.ts,
                formattedLineup
            ));
        }
    }
}
