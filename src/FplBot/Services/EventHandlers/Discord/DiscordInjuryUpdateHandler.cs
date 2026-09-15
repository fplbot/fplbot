using FplBot.Data.Discord;
using FplBot.Domain;
using FplBot.EventHandlers.Discord.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class DiscordInjuryUpdateHandler(IGuildRepository repo, ILogger<DiscordInjuryUpdateHandler> logger)
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
            var subscribedChannels = await repo.GetChannelsSubscribedTo(FplEvent.InjuryUpdates);
            foreach (var (guildId, channelId) in subscribedChannels)
            {
                await context.Publish(new PublishRichToGuildChannel(guildId, channelId, "ℹ️ Injury update", formatted));
            }
        }
        else
        {
            logger.LogInformation("All updates injuries irrelevant, so not sending any notification");
        }
    }
}
