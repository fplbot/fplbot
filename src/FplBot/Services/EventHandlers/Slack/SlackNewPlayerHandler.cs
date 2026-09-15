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
        var filtered = notification.NewPlayers.Where(c => c.IsRelevant());
        if (filtered.Any())
        {
            var formatted = Formatter.FormatNewPlayers(filtered);
            var subscribedChannels = await slackTeamRepo.GetChannelsSubscribedTo(FplEvent.NewPlayers);

            foreach (var (teamId, channelId) in subscribedChannels)
            {
                await context.Publish(new PublishToSlack(teamId, channelId, formatted));
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
        var formatted = Formatter.FormatTransferredPlayers(notification.Transfers);
        var subscribedChannels = await slackTeamRepo.GetChannelsSubscribedTo(FplEvent.NewPlayers);
        foreach (var (teamId, channelId) in subscribedChannels)
        {
            await context.Publish(new PublishToSlack(teamId, channelId, formatted));
        }
    }
}
