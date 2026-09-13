namespace FplBot.Data.Slack;

public interface ISlackTeamRepository
{
    Task<SlackTeam> GetTeam(string teamId);
    Task<SlackTeam?> FindByTeamId(string teamId);
    Task Save(SlackTeam team);
    Task UpdateLeagueId(string teamId, long newLeagueId);
    Task DeleteByTeamId(string teamId);
    Task<IEnumerable<SlackTeam>> GetAllTeams();
    Task UpdateChannel(string teamId, string newChannel);
    Task UpdateSubscriptions(string teamId, IEnumerable<EventSubscription> subscriptions);
}
