using FplBot.Data.Discord;
using FplBot.Domain;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class DiscordNewLeagueEntriesHandler(IGuildRepository repo, ILogger<DiscordNewLeagueEntriesHandler> logger)
    : IConsumer<NewLeagueEntriesRegistered>
{
    public async Task Consume(ConsumeContext<NewLeagueEntriesRegistered> context)
    {
        var message = context.Message;
        logger.LogInformation("Handling {count} new entries in league {leagueId}", message.NewEntries.Count, message.LeagueId);
        if (!message.NewEntries.Any())
        {
            return;
        }

        var formatted = Formatter.FormatNewLeagueEntries(message.LeagueName, message.NewEntries);
        var title = message.NewEntries.Count == 1 ? "🎉 New league entry" : "🎉 New league entries";
        var subscribedChannels = await repo.GetChannelsSubscribedTo(FplEvent.NewLeagueEntries);

        foreach (var (guildId, channelId) in subscribedChannels)
        {
            var channel = await repo.GetChannelSubscription(guildId, channelId);
            if (channel?.FollowedLeagueId?.Value == message.LeagueId)
            {
                await context.Publish(new PublishRichToGuildChannel(guildId, channelId, title, formatted));
            }
        }
    }
}
