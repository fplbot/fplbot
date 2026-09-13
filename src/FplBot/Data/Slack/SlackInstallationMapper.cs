using FplBot.Domain;

namespace FplBot.Data.Slack;

public static class SlackInstallationMapper
{
    // Channel/league/subscription data lives in V2 per-channel storage (SaveChannelSubscription),
    // not on the legacy team-scalar fields below — this only carries team-level account state.
    public static SlackTeam ToStorage(SlackInstallation installation)
    {
        return new SlackTeam
        {
            TeamId = installation.TeamId,
            TeamName = installation.TeamName,
            AccessToken = installation.Token,
            PendingRemoval = installation.PendingRemoval
        };
    }
}
