using FplBot.Data.Discord;
using FplBot.Domain;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class DiscordLikelyPriceChangeHandler(IGuildRepository repo, ILogger<DiscordLikelyPriceChangeHandler> logger)
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
        var subscribedChannels = await repo.GetChannelsSubscribedTo(FplEvent.LikelyPriceChanges, FplEvent.PriceChanges);
        foreach (var (guildId, channelId) in subscribedChannels)
        {
            await context.Publish(new PublishRichToGuildChannel(guildId, channelId, "🔮 Likely price changes", formatted));
        }
    }
}
