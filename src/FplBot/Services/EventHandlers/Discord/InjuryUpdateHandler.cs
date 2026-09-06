using FplBot.Data.Discord;
using FplBot.EventHandlers.Discord.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class InjuryUpdateHandler(IGuildRepository repo, ILogger<InjuryUpdateHandler> logger)
    : IConsumer<InjuryUpdateOccured>
{
    public async Task Consume(ConsumeContext<InjuryUpdateOccured> context)
    {
        var message = context.Message;
        logger.LogInformation($"Handling {message.PlayersWithInjuryUpdates.Count()} injury updates");
        var filtered = message.PlayersWithInjuryUpdates.Where(c => c.Player.IsRelevant());
        if (filtered.Any())
        {
            var formatted = Formatter.FormatInjuryStatusUpdates(filtered);
            var guildSubs = await repo.GetAllGuildSubscriptions();
            foreach (var guildSub in guildSubs)
            {
                if (guildSub.Subscriptions.ContainsSubscriptionFor(EventSubscription.InjuryUpdates))
                {
                    await context.Publish(new PublishRichToGuildChannel(guildSub.GuildId, guildSub.ChannelId, "ℹ️ Injury update", formatted));
                }

            }
        }
        else
        {
            logger.LogInformation("All updates injuries irrelevant, so not sending any notification");
        }
    }
}
