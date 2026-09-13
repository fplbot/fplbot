using FakeItEasy;
using FplBot.ApplicationServices.Slack;
using FplBot.Data;
using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Tests.Helpers;

namespace FplBot.Tests.ApplicationServices.Slack;

public class UninstallSlackWorkspaceTests
{
    private readonly ISlackTeamRepository _repository = A.Fake<ISlackTeamRepository>();
    private readonly TestPublishEndpoint _publishEndpoint = new();
    private readonly UninstallSlackWorkspace _sut;

    public UninstallSlackWorkspaceTests()
    {
        _sut = new UninstallSlackWorkspace(_repository, new TestScopeFactory(_publishEndpoint));
    }

    [Fact]
    public async Task Execute_UnknownTeam_ReturnsNullAndDoesNotDelete()
    {
        A.CallTo(() => _repository.FindByTeamId("T1")).Returns((SlackTeam?)null);

        var result = await _sut.Execute("T1");

        Assert.Null(result);
        A.CallTo(() => _repository.DeleteByTeamId(A<string>._)).MustNotHaveHappened();
        Assert.Empty(_publishEndpoint.PublishedMessages);
    }

    [Fact]
    public async Task Execute_KnownTeam_DeletesReturnsWorkspaceAndPublishesAppUninstalled()
    {
        var team = new SlackTeam
        {
            TeamId = "T1",
            TeamName = "Team One",
            AccessToken = "token1",
            FplBotSlackChannel = "#fpl",
            FplbotLeagueId = 42,
            Subscriptions = new List<EventSubscription> { EventSubscription.Standings }
        };
        A.CallTo(() => _repository.FindByTeamId("T1")).Returns(team);

        var result = await _sut.Execute("T1");

        Assert.NotNull(result);
        Assert.Equal("T1", result.TeamId);
        Assert.Equal("Team One", result.TeamName);
        Assert.Equal("token1", result.Token);
        A.CallTo(() => _repository.DeleteByTeamId("T1")).MustHaveHappenedOnceExactly();

        var published = Assert.Single(_publishEndpoint.PublishedMessages.Containing<AppUninstalled>());
        var appUninstalled = Assert.IsType<AppUninstalled>(published.Message);
        Assert.Equal("T1", appUninstalled.TeamId);
        Assert.Equal("Team One", appUninstalled.TeamName);
    }
}
