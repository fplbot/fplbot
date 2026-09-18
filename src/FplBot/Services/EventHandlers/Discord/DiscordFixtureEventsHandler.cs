using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Data.Discord;
using FplBot.Domain;
using FplBot.Formatting;
using FplBot.Formatting.FixtureStats;
using FplBot.Formatting.Helpers;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class DiscordFixtureEventsHandler(
    IGuildRepository repo,
    ILogger<DiscordFixtureEventsHandler> logger,
    IGlobalSettingsClient globalSettingsClient,
    ILeagueEntriesByGameweek leagueEntriesByGameweek,
    ITransfersByGameWeek transfersByGameWeek)
    : IConsumer<FixtureEventsOccured>, IConsumer<PublishFixtureEventsToGuild>
{
    public async Task Consume(ConsumeContext<FixtureEventsOccured> context)
    {
        var message = context.Message;
        logger.LogInformation($"Handling {message.FixtureEvents.Count} new fixture events");
        var subscribedChannels = await repo.GetChannelsSubscribedTo(FixtureStatEvents);

        foreach (var (guildId, channelId) in subscribedChannels)
        {
            await context.Publish(new PublishFixtureEventsToGuild(guildId, channelId, message.FixtureEvents), ctx => ctx.TimeToLive = TimeSpan.FromMinutes(30));
        }
    }

    private static readonly FplEvent[] FixtureStatEvents =
    [
        FplEvent.FixtureGoals,
        FplEvent.FixtureAssists,
        FplEvent.FixtureCards,
        FplEvent.FixturePenaltyMisses
    ];

    public async Task Consume(ConsumeContext<PublishFixtureEventsToGuild> context)
    {
        var message = context.Message;
        logger.LogInformation($"Publishing {message.FixtureEvents.Count} fixture events to {message.GuildId} and {message.ChannelId}");
        var installation = await repo.FindInstallationByTeamId(message.GuildId);
        var sub = installation?.GetChannel(message.ChannelId);
        if (sub != null)
        {
            TauntData? tauntData = null;
            if (sub.FollowedLeagueId is { } leagueId && sub.IsSubscribedTo(FplEvent.Taunts))
            {
                var gws = await globalSettingsClient.GetGlobalSettings();
                var currentGw = gws?.Gameweeks.GetCurrentGameweek();
                IEnumerable<GameweekEntry> entries = [];
                IEnumerable<TransfersByGameWeek.Transfer> transfers = [];
                if (currentGw != null)
                {
                    entries = await leagueEntriesByGameweek.GetEntriesForGameweek(currentGw.Id, (int)leagueId.Value);
                    transfers = await transfersByGameWeek.GetTransfersByGameweek(currentGw.Id, (int)leagueId.Value);
                }

                tauntData = new TauntData(transfers, entries);
            }

            var eventMessages = GameweekEventsFormatter.FormatNewFixtureEvents(message.FixtureEvents, statType => ChannelHasStat(sub, statType),
                FormattingType.Discord, tauntData);
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

    private static bool ChannelHasStat(ChannelSubscription channel, StatType statType)
    {
        var fplEvent = GetFplEventForStat(statType);
        return fplEvent.HasValue && channel.IsSubscribedTo(fplEvent.Value);
    }

    private static FplEvent? GetFplEventForStat(StatType statType) => statType switch
    {
        StatType.GoalsScored => FplEvent.FixtureGoals,
        StatType.Assists => FplEvent.FixtureAssists,
        StatType.OwnGoals => FplEvent.FixtureGoals,
        StatType.RedCards => FplEvent.FixtureCards,
        StatType.PenaltiesSaved => FplEvent.FixturePenaltyMisses,
        StatType.PenaltiesMissed => FplEvent.FixturePenaltyMisses,
        _ => null
    };
}
