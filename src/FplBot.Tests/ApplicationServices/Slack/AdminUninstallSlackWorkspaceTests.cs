using FakeItEasy;
using FplBot.ApplicationServices.Slack;
using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Tests.Helpers;

namespace FplBot.Tests.ApplicationServices.Slack;

public class AdminUninstallSlackWorkspaceTests
{
    private readonly ISlackTeamRepository _repository = A.Fake<ISlackTeamRepository>();
    private readonly TestPublishEndpoint _publishEndpoint = new();
    private readonly AdminUninstallSlackWorkspace _sut;

    public AdminUninstallSlackWorkspaceTests()
    {
        _sut = new AdminUninstallSlackWorkspace(_repository, new TestScopeFactory(_publishEndpoint));
    }

    [Fact]
    public async Task Execute_UnknownTeam_ReturnsNullAndDoesNotSaveOrPublish()
    {
        A.CallTo(() => _repository.GetTeam("T1")).Returns((SlackTeam)null!);

        var result = await _sut.Execute("T1");

        Assert.Null(result);
        A.CallTo(() => _repository.Save(A<SlackTeam>._)).MustNotHaveHappened();
        Assert.Empty(_publishEndpoint.PublishedMessages);
    }

    [Fact]
    public async Task Execute_KnownTeam_MarksForRemovalSavesAndPublishes()
    {
        var team = new SlackTeam { TeamId = "T1", TeamName = "Team One", AccessToken = "token1" };
        A.CallTo(() => _repository.GetTeam("T1")).Returns(team);

        var result = await _sut.Execute("T1");

        Assert.NotNull(result);
        Assert.Equal("T1", result.TeamId);
        Assert.Equal("Team One", result.TeamName);
        Assert.Equal("token1", result.Token);

        A.CallTo(() => _repository.Save(A<SlackTeam>.That.Matches(t => t.TeamId == "T1" && t.PendingRemoval)))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _repository.DeleteByTeamId(A<string>._)).MustNotHaveHappened();

        var published = Assert.Single(_publishEndpoint.PublishedMessages.Containing<TeamMarkedForRemoval>());
        var evt = Assert.IsType<TeamMarkedForRemoval>(published.Message);
        Assert.Equal("T1", evt.TeamId);
        Assert.Equal("Team One", evt.TeamName);
    }
}
