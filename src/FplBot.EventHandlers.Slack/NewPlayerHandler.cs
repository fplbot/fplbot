using FplBot.Data.Slack;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using NServiceBus;

namespace FplBot.EventHandlers.Slack;

public class NewPlayerHandler : IHandleMessages<NewPlayersRegistered>
{
    private readonly ISlackTeamRepository _slackTeamRepo;
    private readonly ILogger<NewPlayerHandler> _logger;

    public NewPlayerHandler(ISlackTeamRepository slackTeamRepo, ILogger<NewPlayerHandler> logger)
    {
        _slackTeamRepo = slackTeamRepo;
        _logger = logger;
    }

    public async Task Handle(NewPlayersRegistered notification, IMessageHandlerContext context)
    {
        _logger.LogInformation($"Handling {notification.NewPlayers.Count()} new players");
        var slackTeams = await _slackTeamRepo.GetAllTeams();
        var formatted = Formatter.FormatNewPlayers(notification.NewPlayers);
        foreach (var slackTeam in slackTeams)
        {
            if (slackTeam.HasRegisteredFor(EventSubscription.NewPlayers))
            {
                var options = new SendOptions();
                options.RequireImmediateDispatch();
                options.RouteToThisEndpoint();
                await context.Send(new PublishToSlack(slackTeam.TeamId, slackTeam.FplBotSlackChannel, formatted), options);
            }
        }
    }
}
