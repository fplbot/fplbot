using FplBot.Domain;

namespace FplBot.Data.Slack;

public static class SlackInstallationMapper
{


    public static SlackTeam ToStorage(SlackInstallation installation)
    {
        var channel = installation.ChannelSubscriptions.FirstOrDefault();

        return new SlackTeam
        {
            TeamId = installation.TeamId,
            TeamName = installation.TeamName,
            AccessToken = installation.Token,
            FplBotSlackChannel = channel?.ChannelId,
            FplbotLeagueId = channel?.FollowedLeagueId is { } id ? (int)id.Value : null,
            Subscriptions = channel is null
                ? new List<EventSubscription>()
                : channel.Events.Current.Select(ToStorageEvent).ToList(),
            PendingRemoval = installation.PendingRemoval
        };
    }

    private static EventSubscription ToStorageEvent(FplEvent e) => Enum.Parse<EventSubscription>(e.ToString());
}
