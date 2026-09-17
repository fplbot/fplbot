using Fpl.Client.Abstractions;
using FplBot.Data.Discord;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Discord;

public class DiscordNewLeagueEntriesHandler(
    IGuildRepository repo,
    ILeagueClient leagueClient,
    IGlobalSettingsClient globalSettingsClient,
    ILogger<DiscordNewLeagueEntriesHandler> logger)
    : IConsumer<GameweekJustBegan>, IConsumer<ProcessNewLeagueEntriesForGuildChannel>
{
    public async Task Consume(ConsumeContext<GameweekJustBegan> context)
    {
        var gameweekId = context.Message.NewGameweek.Id;
        foreach (var (guildId, channelId, leagueId) in await repo.GetChannelsFollowingALeague())
        {
            await context.Publish(new ProcessNewLeagueEntriesForGuildChannel(guildId, channelId, (int)leagueId.Value, gameweekId));
        }
    }

    public async Task Consume(ConsumeContext<ProcessNewLeagueEntriesForGuildChannel> context)
    {
        var message = context.Message;
        var league = await NewLeagueEntriesLookup.Fetch(leagueClient, globalSettingsClient, message.LeagueId, message.GameweekId, logger);
        if (league is null)
        {
            return;
        }

        logger.LogInformation("Posting {Count} new entries in league {LeagueId} to guild channel {ChannelId}",
            league.Entries.Count, message.LeagueId, message.ChannelId);

        var title = league.Entries.Count > 1 || league.HasMore ? "🎉 New league entries" : "🎉 New league entry";
        var formatted = Formatter.FormatNewLeagueEntries(league.LeagueName, league.Entries, league.HasMore);
        await context.Publish(new PublishRichToGuildChannel(message.GuildId, message.ChannelId, title, formatted));
    }
}
