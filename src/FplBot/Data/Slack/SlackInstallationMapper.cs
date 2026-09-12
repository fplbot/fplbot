using FplBot.Domain;

namespace FplBot.Data.Slack;

public static class SlackInstallationMapper
{
    public static SlackInstallation ToDomain(SlackTeam team)
    {
        var channels = new List<SlackChannelSubscription>();

        if (!string.IsNullOrEmpty(team.FplBotSlackChannel))
        {
            var leagueId = team.FplbotLeagueId is { } id ? new ClassicLeagueId(id) : null;
            var events = team.Subscriptions.Select(ToDomainEvent);
            channels.Add(SlackChannelSubscription.Reconstitute(team.FplBotSlackChannel, leagueId, events));
        }

        return SlackInstallation.Reconstitute(team.TeamId!, team.AccessToken ?? string.Empty, channels);
    }

    public static SlackTeam ToStorage(SlackInstallation installation, string? teamName)
    {
        var channel = installation.ChannelSubscriptions.FirstOrDefault();

        return new SlackTeam
        {
            TeamId = installation.TeamId,
            TeamName = teamName,
            AccessToken = installation.Token,
            FplBotSlackChannel = channel?.ChannelId,
            FplbotLeagueId = channel?.FollowedLeagueId is { } id ? (int)id.Value : null,
            Subscriptions = channel is null
                ? new List<EventSubscription>()
                : channel.Events.Current.Select(ToStorageEvent).ToList()
        };
    }

    private static FplEvent ToDomainEvent(EventSubscription e) => Enum.Parse<FplEvent>(e.ToString());
    private static EventSubscription ToStorageEvent(FplEvent e) => Enum.Parse<EventSubscription>(e.ToString());
}
