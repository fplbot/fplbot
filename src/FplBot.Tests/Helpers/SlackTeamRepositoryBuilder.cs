using FakeItEasy;
using FplBot.Data.Slack;

namespace FplBot.Tests.Helpers;

public class SlackTeamRepositoryBuilder
{
    private readonly List<SlackTeam> _teams = new();

    public SlackTeamRepositoryBuilder WithTeam(SlackTeam team)
    {
        _teams.Add(team);
        return this;
    }

    public ISlackTeamRepository Build()
    {
        var teams = _teams.Count > 0 ? _teams : new List<SlackTeam> { DefaultTeam() };

        var fake = A.Fake<ISlackTeamRepository>();
        A.CallTo(() => fake.GetTeam(A<string>._))
            .ReturnsLazily((string teamId) => Task.FromResult(teams.FirstOrDefault(t => t.TeamId == teamId) ?? teams[0]));
        A.CallTo(() => fake.GetAllTeams())
            .Returns(Task.FromResult<IEnumerable<SlackTeam>>(teams));
        A.CallTo(() => fake.UpdateLeagueId(A<string>._, A<long>._)).Returns(Task.CompletedTask);
        A.CallTo(() => fake.UpdateChannel(A<string>._, A<string>._)).Returns(Task.CompletedTask);
        A.CallTo(() => fake.UpdateSubscriptions(A<string>._, A<IEnumerable<EventSubscription>>._)).Returns(Task.CompletedTask);
        return fake;
    }

    private static SlackTeam DefaultTeam()
    {
        return new SlackTeam
        {
            FplbotLeagueId = 15263,
            FplBotSlackChannel = "#lol",
            Subscriptions = []
        };
    }
}
