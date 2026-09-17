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
    IGlobalSettingsClient globalSettingsClient,
    ILogger<SlackNewLeagueEntriesHandler> logger)
    : IConsumer<OneHourToDeadline>, IConsumer<ProcessNewLeagueEntriesForSlackChannel>
{
    public async Task Consume(ConsumeContext<OneHourToDeadline> context)
    {
        var gameweekId = context.Message.GameweekNearingDeadline.Id;
        foreach (var (teamId, channelId, leagueId) in await slackTeamRepo.GetChannelsFollowingALeague())
        {
            await context.Publish(new ProcessNewLeagueEntriesForSlackChannel(teamId, channelId, (int)leagueId.Value, gameweekId));
        }
    }

    public async Task Consume(ConsumeContext<ProcessNewLeagueEntriesForSlackChannel> context)
    {
        var message = context.Message;
        var league = await NewLeagueEntriesLookup.Fetch(leagueClient, globalSettingsClient, message.LeagueId, message.GameweekId, logger);
        if (league is null)
        {
            return;
        }

        logger.LogInformation("Posting {Count} new entries in league {LeagueId} to slack channel {ChannelId}",
            league.Entries.Count, message.LeagueId, message.ChannelId);

        var formatted = Formatter.FormatNewLeagueEntries(league.LeagueName, league.Entries, league.HasMore);
        await context.Publish(new PublishToSlack(message.WorkspaceId, message.ChannelId, formatted));
    }
}
