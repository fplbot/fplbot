using FplBot.Data.Discord;
using FplBot.EventHandlers.Discord.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class NewPlayersHandler(IGuildRepository repo, ILogger<InjuryUpdateHandler> logger)
    : IConsumer<NewPlayersRegistered>, IConsumer<PremiershipPlayerTransferred>
{
    public async Task Consume(ConsumeContext<NewPlayersRegistered> context)
    {
        var message = context.Message;
        logger.LogInformation($"Handling {message.NewPlayers.Count()} new players");

        var filtered = message.NewPlayers.Where(c => c.IsRelevant());
        if (filtered.Any())
        {
            var guildSubs = await repo.GetAllGuildSubscriptions();
            var formatted = Formatter.FormatNewPlayers(filtered);

            foreach (var guildSub in guildSubs)
            {
                if (guildSub.Subscriptions.ContainsSubscriptionFor(EventSubscription.NewPlayers) && !string.IsNullOrEmpty(formatted))
                {
                    await context.Publish(new PublishRichToGuildChannel(guildSub.GuildId, guildSub.ChannelId,"ℹ️ New players", formatted));
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
        var message = context.Message;
        logger.LogInformation($"Handling {message.Transfers.Count()} new transfers");
        var guildSubs = await repo.GetAllGuildSubscriptions();
        var formatted = Formatter.FormatTransferredPlayers(message.Transfers, includeheader:false);

        foreach (var guildSub in guildSubs)
        {
            if (guildSub.Subscriptions.ContainsSubscriptionFor(EventSubscription.NewPlayers) && !string.IsNullOrEmpty(formatted))
            {
                await context.Publish(new PublishRichToGuildChannel(guildSub.GuildId, guildSub.ChannelId,"🔄️ Transfer!", formatted));
            }
        }
    }
}
