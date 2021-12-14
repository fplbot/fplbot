using FplBot.Data.Slack;

namespace FplBot.Tests.Helpers;

public class InMemorySlackTeamRepository : ISlackTeamRepository
{
    private readonly int _leagueId;

    public InMemorySlackTeamRepository()
    {
        _leagueId = 15263;
    }

    public Task<SlackTeam> GetTeam(string teamId)
    {
        return Task.FromResult(new SlackTeam
        {
            Subscriptions = new EventSubscription[0],
            FplBotSlackChannel = "#lol",
            FplbotLeagueId = _leagueId
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
                FplbotLeagueId = _leagueId,
                FplBotSlackChannel = "#lol",
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
