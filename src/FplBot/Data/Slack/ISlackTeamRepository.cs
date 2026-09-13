using FplBot.Domain;

namespace FplBot.Data.Slack;

public interface ISlackTeamRepository
{
    Task<SlackInstallation> GetInstallation(string teamId);
    Task Save(SlackInstallation installation);
    Task<SlackInstallation?> FindInstallationByTeamId(string teamId);
    Task<IEnumerable<SlackInstallation>> GetAllInstallations();

    Task UpdateLeagueId(string teamId, long newLeagueId);
    Task DeleteByTeamId(string teamId);
    Task UpdateChannel(string teamId, string newChannel);
    Task UpdateSubscriptions(string teamId, IEnumerable<EventSubscription> subscriptions);
    Task<IEnumerable<SlackTeam>> GetAllTeamsLegacyDoNotUse();

    // V2 storage: one SlackChannelSubscriptionRecord per channel, replacing the single
    // scalar Channel/LeagueId/Subscriptions fields V1 stores per team.
    Task SaveChannelSubscription(string teamId, SlackChannelSubscription channel);
    Task<IEnumerable<SlackChannelSubscription>> GetChannelSubscriptions(string teamId);
}
