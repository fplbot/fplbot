using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data.Discord;
using FplBot.EventHandlers.Discord.Helpers;
using FplBot.Formatting;
using FplBot.Formatting.FixtureStats;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class FixtureEventsHandler(
    IGuildRepository repo,
    ILogger<FixtureEventsHandler> logger,
    IGlobalSettingsClient globalSettingsClient,
    ILeagueEntriesByGameweek leagueEntriesByGameweek,
    ITransfersByGameWeek transfersByGameWeek)
    : IConsumer<FixtureEventsOccured>, IConsumer<PublishFixtureEventsToGuild>
{
    public async Task Consume(ConsumeContext<FixtureEventsOccured> context)
    {
        var message = context.Message;
        logger.LogInformation($"Handling {message.FixtureEvents.Count} new fixture events");
        var subs = await repo.GetAllGuildSubscriptions();

        foreach (var sub in subs)
        {
            await context.Publish(new PublishFixtureEventsToGuild(sub.GuildId, sub.ChannelId, message.FixtureEvents), ctx => ctx.TimeToLive = TimeSpan.FromMinutes(30));
        }
    }

    public async Task Consume(ConsumeContext<PublishFixtureEventsToGuild> context)
    {
        var message = context.Message;
        logger.LogInformation($"Publishing {message.FixtureEvents.Count} fixture events to {message.GuildId} and {message.ChannelId}");
        var sub = await repo.GetGuildSubscription(message.GuildId, message.ChannelId);
        if (sub != null)
        {
            TauntData? tauntData = null;
            if (sub.LeagueId.HasValue && sub.Subscriptions.ContainsSubscriptionFor(EventSubscription.Taunts))
            {
                var gws = await globalSettingsClient.GetGlobalSettings();
                var currentGw = gws?.Gameweeks.GetCurrentGameweek();
                IEnumerable<GameweekEntry> entries = new List<GameweekEntry>();
                IEnumerable<TransfersByGameWeek.Transfer> transfers = new List<TransfersByGameWeek.Transfer>();
                if (currentGw != null)
                {
                    entries = await leagueEntriesByGameweek.GetEntriesForGameweek(currentGw.Id, sub.LeagueId.Value);
                    transfers = await transfersByGameWeek.GetTransfersByGameweek(currentGw.Id, sub.LeagueId.Value);
                }

                tauntData = new TauntData(transfers, entries);
            }
            var eventMessages = GameweekEventsFormatter.FormatNewFixtureEvents(message.FixtureEvents, sub.Subscriptions.ContainsStat, FormattingType.Discord, tauntData);
            foreach (var eventMsg in eventMessages)
            {
                await context.Publish(new PublishRichToGuildChannel(message.GuildId, message.ChannelId, eventMsg.Title, eventMsg.Details));
            }
        }
        else
        {
            logger.LogInformation($"Guild {message.GuildId} in channel {message.ChannelId} not subbing to fixture events. Not sending");
        }
    }
}
