using FplBot.Data.Discord;
using FplBot.EventHandlers.Discord.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class PriceChangeHandler(IGuildRepository repo, ILogger<PriceChangeHandler> logger)
    : IConsumer<PlayersPriceChanged>
{
    public async Task Consume(ConsumeContext<PlayersPriceChanged> context)
    {
        var notification = context.Message;
        logger.LogInformation($"Handling {notification.PlayersWithPriceChanges.Count()} price updates");
        var guildSubs = await repo.GetAllGuildSubscriptions();
        var filtered = notification.PlayersWithPriceChanges.Where(c => c.IsRelevant());

        if (filtered.Any())
        {
            var formatted = Formatter.FormatPriceChanged(filtered);

            foreach (var guildSub in guildSubs)
            {
                if (guildSub.Subscriptions.ContainsSubscriptionFor(EventSubscription.PriceChanges) && !string.IsNullOrEmpty(formatted))
                {
                    await context.Publish(new PublishRichToGuildChannel(guildSub.GuildId, guildSub.ChannelId, "ℹ️ Price changes", formatted));
                }
            }
        }
    }
}
