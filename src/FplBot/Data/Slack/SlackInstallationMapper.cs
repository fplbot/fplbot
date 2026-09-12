using FplBot.Domain;

namespace FplBot.Data.Slack;

public static class SlackInstallationMapper
{
    public static SlackInstallation ToDomain(SlackTeam team)
    {
        var installation = SlackInstallation.Install(team.TeamId!, team.AccessToken ?? string.Empty);

        if (!string.IsNullOrEmpty(team.FplBotSlackChannel))
        {
            if (team.FplbotLeagueId is { } leagueId)
            {
                installation.Follow(team.FplBotSlackChannel, new ClassicLeagueId(leagueId));
            }

            var events = team.Subscriptions.Select(ToDomainEvent).ToArray();
            if (events.Length > 0)
            {
                installation.Subscribe(team.FplBotSlackChannel, events);
            }
        }

        return installation;
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
