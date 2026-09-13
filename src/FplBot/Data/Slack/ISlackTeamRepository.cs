using FplBot.Domain;

namespace FplBot.Data.Slack;

public interface ISlackTeamRepository
{
    Task<SlackInstallation> GetInstallation(string teamId);
    Task Save(SlackInstallation installation);
    Task<SlackInstallation?> FindInstallationByTeamId(string teamId);
    Task<IEnumerable<SlackInstallation>> GetAllInstallations();

    Task Delete(SlackInstallation installation);

    // V2 storage: one SlackChannelSubscriptionRecord per channel, replacing the single
    // scalar Channel/LeagueId/Subscriptions fields V1 stores per team.
    Task<IEnumerable<SlackChannelSubscription>> GetChannelSubscriptions(string teamId);
    Task<IEnumerable<SlackTeam>> GetAllTeamsLegacyDoNotUse();
    Task<SlackTeam?> FindTeamLegacyDoNotUse(string teamId);
}
