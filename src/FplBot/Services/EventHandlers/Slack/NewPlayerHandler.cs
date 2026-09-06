using FplBot.Data.Slack;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack;

public class NewPlayerHandler : IConsumer<NewPlayersRegistered>, IConsumer<PremiershipPlayerTransferred>
{
    private readonly ISlackTeamRepository _slackTeamRepo;
    private readonly ILogger<NewPlayerHandler> _logger;

    public NewPlayerHandler(ISlackTeamRepository slackTeamRepo, ILogger<NewPlayerHandler> logger)
    {
        _slackTeamRepo = slackTeamRepo;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<NewPlayersRegistered> context)
    {
        var notification = context.Message;
        _logger.LogInformation($"Handling {notification.NewPlayers.Count()} new players");
        var slackTeams = await _slackTeamRepo.GetAllTeams();
        var filtered = notification.NewPlayers.Where(c => c.IsRelevant());
        if (filtered.Any())
        {
            var formatted = Formatter.FormatNewPlayers(filtered);

            foreach (var slackTeam in slackTeams)
            {
                if (slackTeam.HasRegisteredFor(EventSubscription.NewPlayers))
                {
                    await context.Publish(new PublishToSlack(slackTeam.TeamId!, slackTeam.FplBotSlackChannel!, formatted));
                }
            }
        }
        else
        {
            _logger.LogInformation("All new players irrelevant, so not sending any notification");
        }
    }

    public async Task Consume(ConsumeContext<PremiershipPlayerTransferred> context)
    {
        var notification = context.Message;
        _logger.LogInformation($"Handling {notification.Transfers.Count()} new transfers");
        var slackTeams = await _slackTeamRepo.GetAllTeams();
        var formatted = Formatter.FormatTransferredPlayers(notification.Transfers);
        foreach (var slackTeam in slackTeams)
        {
            if (slackTeam.HasRegisteredFor(EventSubscription.NewPlayers))
            {
                await context.Publish(new PublishToSlack(slackTeam.TeamId!, slackTeam.FplBotSlackChannel!, formatted));
            }
        }
    }
}
