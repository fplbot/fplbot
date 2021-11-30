using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Slack.Abstractions;
using FplBot.Slack.Data.Abstractions;
using FplBot.Slack.Data.Models;
using Microsoft.Extensions.Logging;
using NServiceBus;

namespace FplBot.Slack.Handlers.FplEvents;

public class NewPlayerHandler : IHandleMessages<NewPlayersRegistered>, IHandleMessages<PublishNewPlayersToSlackWorkspace>
{
    private readonly ISlackWorkSpacePublisher _publisher;
    private readonly ISlackTeamRepository _slackTeamRepo;
    private readonly ILogger<NewPlayerHandler> _logger;

    public NewPlayerHandler(ISlackWorkSpacePublisher publisher, ISlackTeamRepository slackTeamRepo, ILogger<NewPlayerHandler> logger)
    {
        _publisher = publisher;
        _slackTeamRepo = slackTeamRepo;
        _logger = logger;
    }

    public async Task Handle(NewPlayersRegistered notification, IMessageHandlerContext context)
    {
        _logger.LogInformation($"Handling {notification.NewPlayers.Count()} new players");
        var slackTeams = await _slackTeamRepo.GetAllTeams();
        foreach (var slackTeam in slackTeams)
        {
            if (slackTeam.HasRegisteredFor(EventSubscription.NewPlayers))
            {
                var options = new SendOptions();
                options.RequireImmediateDispatch();
                options.RouteToThisEndpoint();
                await context.Send(new PublishNewPlayersToSlackWorkspace(slackTeam.TeamId, notification.NewPlayers), options);
            }
        }
    }

    public async Task Handle(PublishNewPlayersToSlackWorkspace message, IMessageHandlerContext context)
    {
        _logger.LogInformation($"Publishing message to {message.WorkspaceId}");
        var workspace = await _slackTeamRepo.GetTeam(message.WorkspaceId);
        var formatted = Formatter.FormatNewPlayers(message.NewPlayers);
        await _publisher.PublishToWorkspace(message.WorkspaceId, workspace.FplBotSlackChannel, formatted);
    }
}
