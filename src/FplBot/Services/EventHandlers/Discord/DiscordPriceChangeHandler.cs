using FplBot.Data.Discord;
using FplBot.Domain;
using FplBot.EventHandlers.Discord.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class DiscordPriceChangeHandler(IGuildRepository repo, ILogger<DiscordPriceChangeHandler> logger)
    : IConsumer<PlayersPriceChanged>
{
    public async Task Consume(ConsumeContext<PlayersPriceChanged> context)
    {
        var notification = context.Message;
        logger.LogInformation($"Handling {notification.PlayersWithPriceChanges.Count()} price updates");
        var subscribedChannels = await repo.GetChannelsSubscribedTo(FplEvent.PriceChanges);
        var filtered = notification.PlayersWithPriceChanges.Where(c => c.IsRelevant());

        if (filtered.Any())
        {
            var formatted = Formatter.FormatPriceChanged(filtered);

            if (!string.IsNullOrEmpty(formatted))
            {
                foreach (var (guildId, channelId) in subscribedChannels)
                {
                    await context.Publish(new PublishRichToGuildChannel(guildId, channelId, "ℹ️ Price changes", formatted));
                }
            }
        }
    }
}
