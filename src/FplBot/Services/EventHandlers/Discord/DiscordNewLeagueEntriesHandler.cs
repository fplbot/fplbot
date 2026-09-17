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
    ILogger<DiscordNewLeagueEntriesHandler> logger)
    : IConsumer<GameweekJustBegan>
{
    public async Task Consume(ConsumeContext<GameweekJustBegan> context)
    {
        var resolved = await NewLeagueEntries.ResolveForFollowedLeagues(repo, leagueClient, context.Message.NewGameweek.Id, logger);
        logger.LogInformation("Handling new league entries for {Count} guild channels", resolved.Count);

        foreach (var channel in resolved)
        {
            var title = channel.Entries.Count > 1 || channel.HasMore ? "🎉 New league entries" : "🎉 New league entry";
            var formatted = Formatter.FormatNewLeagueEntries(channel.LeagueName, channel.Entries, channel.HasMore);
            await context.Publish(new PublishRichToGuildChannel(channel.InstallationId, channel.ChannelId, title, formatted));
        }
    }
}
