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
}
