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
            var installations = await repo.GetAllInstallations();
            foreach (var installation in installations)
            {
                foreach (var channel in installation.GetSubscriptionsTo(FplEvent.InjuryUpdates))
                {
                    await context.Publish(new PublishRichToGuildChannel(installation.Id, channel.ChannelId, "ℹ️ Injury update", formatted));
                }
            }
        }
        else
        {
            logger.LogInformation("All updates injuries irrelevant, so not sending any notification");
        }
    }
}
