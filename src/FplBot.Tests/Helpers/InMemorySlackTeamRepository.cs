using FplBot.Data.Slack;

namespace FplBot.Tests.Helpers;

// Fake team fields (channel/league id/name) intentionally match FplBot.AppHost's
// DevSeederLifecycleHook "TeamId-DEV-SLACK" seed, so there's one consistent example
// fake Slack team across unit tests and local dev instead of unrelated arbitrary values.
public class InMemorySlackTeamRepository : ISlackTeamRepository
{
    private const int LeagueId = 12345;
    private const string Channel = "C0DEV000001";
    private const string TeamName = "Dev Slack Workspace";

    public Task<SlackTeam> GetTeam(string teamId)
    {
        return Task.FromResult(new SlackTeam
        {
            TeamId = teamId,
            TeamName = TeamName,
            Subscriptions = [],
            FplBotSlackChannel = Channel,
            FplbotLeagueId = LeagueId
        });
    }

    public Task UpdateLeagueId(string teamId, long newLeagueId)
    {
        return Task.CompletedTask;
    }

    public Task DeleteByTeamId(string teamId)
    {
        throw new System.NotImplementedException();
    }

    public Task<IEnumerable<SlackTeam>> GetAllTeams()
    {
        IEnumerable<SlackTeam> teams = new []{
            new SlackTeam
            {
                TeamId = "DEV-SLACK",
                TeamName = TeamName,
                FplbotLeagueId = LeagueId,
                FplBotSlackChannel = Channel,
                Subscriptions = new EventSubscription[0]
            }
        };
        return Task.FromResult(teams);
    }

    public Task UpdateChannel(string teamId, string newChannel)
    {
        return Task.CompletedTask;
    }

    public Task UpdateSubscriptions(string teamId, IEnumerable<EventSubscription> subscriptions)
    {
        return Task.CompletedTask;
    }
}
