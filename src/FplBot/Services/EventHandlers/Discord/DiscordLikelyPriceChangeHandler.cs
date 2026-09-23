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

        var subscribedChannels = await repo.GetChannelsSubscribedTo(FplEvent.LikelyPriceChanges);
        foreach (var (guildId, channelId) in subscribedChannels)
        {
            await context.Publish(new PublishRichToGuildChannel(guildId, channelId, "🔮 Likely price changes", formatted));
        }
    }
}
