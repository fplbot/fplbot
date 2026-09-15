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
            var installations = await repo.GetAllInstallations();
            var formatted = Formatter.FormatNewPlayers(filtered);

            foreach (var installation in installations)
            {
                foreach (var channel in installation.GetSubscriptionsTo(FplEvent.NewPlayers))
                {
                    if (!string.IsNullOrEmpty(formatted))
                    {
                        await context.Publish(new PublishRichToGuildChannel(installation.Id, channel.ChannelId, "ℹ️ New players", formatted));
                    }
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
        var installations = await repo.GetAllInstallations();
        var formatted = Formatter.FormatTransferredPlayers(message.Transfers, includeheader:false);

        foreach (var installation in installations)
        {
            foreach (var channel in installation.GetSubscriptionsTo(FplEvent.NewPlayers))
            {
                if (!string.IsNullOrEmpty(formatted))
                {
                    await context.Publish(new PublishRichToGuildChannel(installation.Id, channel.ChannelId, "🔄️ Transfer!", formatted));
                }
            }
        }
    }
}
