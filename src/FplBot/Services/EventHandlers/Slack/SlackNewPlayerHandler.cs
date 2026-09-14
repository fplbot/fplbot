using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.EventHandlers.Slack.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack;

public class SlackNewPlayerHandler(ISlackTeamRepository slackTeamRepo, ILogger<SlackNewPlayerHandler> logger)
    : IConsumer<NewPlayersRegistered>, IConsumer<PremiershipPlayerTransferred>
{
    public async Task Consume(ConsumeContext<NewPlayersRegistered> context)
    {
        var notification = context.Message;
        logger.LogInformation($"Handling {notification.NewPlayers.Count()} new players");
        var installations = await slackTeamRepo.GetAllInstallations();
        var filtered = notification.NewPlayers.Where(c => c.IsRelevant());
        if (filtered.Any())
        {
            var formatted = Formatter.FormatNewPlayers(filtered);

            foreach (var installation in installations)
            {
                foreach (var channel in installation.GetSubscriptionsTo(FplEvent.NewPlayers))
                {
                    await context.Publish(new PublishToSlack(installation.Id, channel.ChannelId, formatted));
                }
            }
        }
        else
        {
            logger.LogInformation("All new players irrelevant, so not sending any notification");
        }
    }

    public async Task Consume(ConsumeContext<PremiershipPlayerTransferred> context)
    {
        var notification = context.Message;
        logger.LogInformation($"Handling {notification.Transfers.Count()} new transfers");
        var installations = await slackTeamRepo.GetAllInstallations();
        var formatted = Formatter.FormatTransferredPlayers(notification.Transfers);
        foreach (var installation in installations)
        {
            foreach (var channel in installation.GetSubscriptionsTo(FplEvent.NewPlayers))
            {
                await context.Publish(new PublishToSlack(installation.Id, channel.ChannelId, formatted));
            }
        }
    }
}
