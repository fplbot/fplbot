using FplBot.Data.Slack;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack;

public class InjuryUpdateHandler(ISlackTeamRepository slackTeamRepo, ILogger<InjuryUpdateHandler> logger)
    : IConsumer<InjuryUpdateOccured>
{
    public async Task Consume(ConsumeContext<InjuryUpdateOccured> context)
    {
        var notification = context.Message;
        logger.LogInformation($"Handling {notification.PlayersWithInjuryUpdates.Count()} injury updates");
        var filtered = notification.PlayersWithInjuryUpdates.Where(c => c.Player.IsRelevant());
        if (filtered.Any())
        {
            var formatted = Formatter.FormatInjuryStatusUpdates(filtered);
            var slackTeams = await slackTeamRepo.GetAllTeams();
            foreach (var slackTeam in slackTeams)
            {
                if (slackTeam.HasRegisteredFor(EventSubscription.InjuryUpdates))
                {
                    await context.Publish(new PublishToSlack(slackTeam.TeamId!, slackTeam.FplBotSlackChannel!, formatted));
                }
            }
        }
        else
        {
            logger.LogInformation("All updates injuries irrelevant, so not sending any notification");
        }
    }
}
