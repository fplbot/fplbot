using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Formatting;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;

namespace FplBot.EventHandlers.Slack;

internal class SlackNewLeagueEntriesHandler(ISlackTeamRepository slackTeamRepo, ILogger<SlackNewLeagueEntriesHandler> logger)
    : IConsumer<NewLeagueEntriesRegistered>
{
    public async Task Consume(ConsumeContext<NewLeagueEntriesRegistered> context)
    {
        var notification = context.Message;
        logger.LogInformation("Handling {count} new entries in league {leagueId}", notification.NewEntries.Count, notification.LeagueId);
        if (!notification.NewEntries.Any())
        {
            return;
        }

        var formatted = Formatter.FormatNewLeagueEntries(notification.LeagueName, notification.NewEntries);
        var subscribedChannels = await slackTeamRepo.GetChannelsSubscribedTo(FplEvent.NewLeagueEntries);

        foreach (var (teamId, channelId) in subscribedChannels)
        {
            var channel = await slackTeamRepo.GetChannelSubscription(teamId, channelId);
            if (channel?.FollowedLeagueId?.Value == notification.LeagueId)
            {
                await context.Publish(new PublishToSlack(teamId, channelId, formatted));
            }
        }
    }
}
