using FplBot.Data.Slack;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.EventHandlers.Slack;

public class LineupReadyHandler(
    ISlackTeamRepository slackTeamRepo,
    ISlackClientBuilder builder,
    ILogger<LineupReadyHandler> logger)
    : IConsumer<LineupReady>, IConsumer<PublishLineupsToSlackWorkspace>
{
    public async Task Consume(ConsumeContext<LineupReady> context)
    {
        var message = context.Message;
        logger.LogInformation("Handling new lineups");
        var slackTeams = await slackTeamRepo.GetAllTeams();

        foreach (var slackTeam in slackTeams)
        {
            if (slackTeam.HasRegisteredFor(EventSubscription.Lineups))
            {
                await context.Publish(new PublishLineupsToSlackWorkspace(slackTeam.TeamId!, message.Lineup));
            }
        }
    }

    public async Task Consume(ConsumeContext<PublishLineupsToSlackWorkspace> context)
    {
        var message = context.Message;
        var team = await slackTeamRepo.GetTeam(message.WorkspaceId);
        var slackClient = builder.Build(team.AccessToken);
        var lineups = message.Lineups;
        var firstMessage = $"*Lineups {lineups.HomeTeamLineup.TeamName}-{lineups.AwayTeamLineup.TeamName} ready* 👇";

        var res = await slackClient.ChatPostMessage(team.FplBotSlackChannel, firstMessage);
        if (res.Ok)
        {
            var formattedLineup = Formatter.FormatLineup(lineups);
            await context.Publish(new PublishSlackThreadMessage
            (
                message.WorkspaceId,
                team.FplBotSlackChannel!,
                res.ts,
                formattedLineup
            ));
        }
    }
}
