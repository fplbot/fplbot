using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Slack.Data.Abstractions;
using FplBot.Slack.Data.Models;
using Microsoft.Extensions.Logging;
using NServiceBus;
using Slackbot.Net.SlackClients.Http;

namespace FplBot.Slack.Handlers.FplEvents;

public class LineupReadyHandler : IHandleMessages<LineupReady>, IHandleMessages<PublishLineupsToSlackWorkspace>
{
    private readonly ISlackTeamRepository _slackTeamRepo;
    private readonly ISlackClientBuilder _builder;
    private readonly ILogger<LineupReadyHandler> _logger;

    public LineupReadyHandler(ISlackTeamRepository slackTeamRepo, ISlackClientBuilder builder, ILogger<LineupReadyHandler> logger)
    {
        _slackTeamRepo = slackTeamRepo;
        _builder = builder;
        _logger = logger;
    }

    public async Task Handle(LineupReady message, IMessageHandlerContext context)
    {
        _logger.LogInformation("Handling new lineups");
        var slackTeams = await _slackTeamRepo.GetAllTeams();

        foreach (var slackTeam in slackTeams)
        {
            if (slackTeam.HasRegisteredFor(EventSubscription.Lineups))
            {
                var options = new SendOptions();
                options.RequireImmediateDispatch();
                options.RouteToThisEndpoint();
                await context.Send(new PublishLineupsToSlackWorkspace(slackTeam.TeamId, message.Lineup), options);
            }
        }
    }

    public async Task Handle(PublishLineupsToSlackWorkspace message, IMessageHandlerContext context)
    {
        var team = await _slackTeamRepo.GetTeam(message.WorkspaceId);
        var slackClient = _builder.Build(team.AccessToken);
        var lineups = message.Lineups;
        var firstMessage = $"*Lineups {lineups.HomeTeamLineup.TeamName}-{lineups.AwayTeamLineup.TeamName} ready* 👇";

        var res = await slackClient.ChatPostMessage(team.FplBotSlackChannel, firstMessage);
        if (res.Ok)
        {
            var formattedLineup = Formatter.FormatLineup(lineups);
            var options = new SendOptions();
            options.RequireImmediateDispatch();
            options.RouteToThisEndpoint();
            await context.Send(new PublishSlackThreadMessage
            (
                message.WorkspaceId,
                team.FplBotSlackChannel,
                res.ts,
                formattedLineup
            ),options);
        }
    }
}
