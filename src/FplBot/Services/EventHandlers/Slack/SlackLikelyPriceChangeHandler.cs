using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack;

public class SlackLikelyPriceChangeHandler(ISlackTeamRepository slackTeamRepo, ILogger<SlackLikelyPriceChangeHandler> logger)
    : IConsumer<PlayersLikelyToChangePrice>
{
    public async Task Consume(ConsumeContext<PlayersLikelyToChangePrice> context)
    {
        var notification = context.Message;
        logger.LogInformation($"Handling {notification.Players.Count} likely price changes");
        if (notification.Players.Count == 0)
            return;

        var formatted = Formatter.FormatLikelyPriceChanges(notification.Players);
        if (string.IsNullOrEmpty(formatted))
            return;

        var subscribedChannels = await slackTeamRepo.GetChannelsSubscribedTo(FplEvent.LikelyPriceChanges);
        foreach (var (teamId, channelId) in subscribedChannels)
        {
            await context.Publish(new PublishToSlack(teamId, channelId, formatted));
        }
    }
}
