using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack;

public class SlackInjuryUpdateHandler(ISlackTeamRepository slackTeamRepo, ILogger<SlackInjuryUpdateHandler> logger)
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
            var installations = await slackTeamRepo.GetAllInstallations();
            foreach (var installation in installations)
            {
                foreach (var channel in installation.GetSubscriptionsTo(FplEvent.InjuryUpdates))
                {
                    await context.Publish(new PublishToSlack(installation.TeamId, channel.ChannelId, formatted));
                }
            }
        }
        else
        {
            logger.LogInformation("All updates injuries irrelevant, so not sending any notification");
        }
    }
}
