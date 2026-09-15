using FplBot.Data.Discord;
using FplBot.Domain;
using FplBot.EventHandlers.Discord.Helpers;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class DiscordNewPlayersHandler(IGuildRepository repo, ILogger<DiscordNewPlayersHandler> logger)
    : IConsumer<NewPlayersRegistered>, IConsumer<PremiershipPlayerTransferred>
{
    public async Task Consume(ConsumeContext<NewPlayersRegistered> context)
    {
        var message = context.Message;
        logger.LogInformation($"Handling {message.NewPlayers.Count()} new players");

        var filtered = message.NewPlayers.Where(c => c.IsRelevant());
        if (filtered.Any())
        {
            var subscribedChannels = await repo.GetChannelsSubscribedTo(FplEvent.NewPlayers);
            var formatted = Formatter.FormatNewPlayers(filtered);

            foreach (var (guildId, channelId) in subscribedChannels)
            {
                if (!string.IsNullOrEmpty(formatted))
                {
                    await context.Publish(new PublishRichToGuildChannel(guildId, channelId, "ℹ️ New players", formatted));
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
        var subscribedChannels = await repo.GetChannelsSubscribedTo(FplEvent.NewPlayers);
        var formatted = Formatter.FormatTransferredPlayers(message.Transfers, includeheader:false);

        foreach (var (guildId, channelId) in subscribedChannels)
        {
            if (!string.IsNullOrEmpty(formatted))
            {
                await context.Publish(new PublishRichToGuildChannel(guildId, channelId, "🔄️ Transfer!", formatted));
            }
        }
    }
}
