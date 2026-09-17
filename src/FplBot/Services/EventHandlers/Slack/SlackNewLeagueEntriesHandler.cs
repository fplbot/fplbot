using Fpl.Client.Abstractions;
using FplBot.Data.Slack;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack;

public class SlackNewLeagueEntriesHandler(
    ISlackTeamRepository slackTeamRepo,
    ILeagueClient leagueClient,
    ILogger<SlackNewLeagueEntriesHandler> logger)
    : IConsumer<GameweekJustBegan>
{
    public async Task Consume(ConsumeContext<GameweekJustBegan> context)
    {
        var resolved = await NewLeagueEntries.ResolveForFollowedLeagues(slackTeamRepo, leagueClient, context.Message.NewGameweek.Id, logger);
        logger.LogInformation("Handling new league entries for {Count} slack channels", resolved.Count);

        foreach (var channel in resolved)
        {
            var formatted = Formatter.FormatNewLeagueEntries(channel.LeagueName, channel.Entries, channel.HasMore);
            await context.Publish(new PublishToSlack(channel.InstallationId, channel.ChannelId, formatted));
        }
    }
}
