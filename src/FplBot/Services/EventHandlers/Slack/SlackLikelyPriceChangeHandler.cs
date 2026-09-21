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

        // Also shown to existing PriceChanges subscribers for now, as a showcase — remove once the audience has grown.
        var subscribedChannels = await slackTeamRepo.GetChannelsSubscribedTo(FplEvent.LikelyPriceChanges, FplEvent.PriceChanges);
        foreach (var (teamId, channelId) in subscribedChannels)
        {
            await context.Publish(new PublishToSlack(teamId, channelId, formatted));
        }
    }
}
