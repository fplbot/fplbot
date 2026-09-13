using FakeItEasy;
using FplBot.ApplicationServices.Slack;
using FplBot.Data.Slack;

namespace FplBot.Tests.ApplicationServices.Slack;

public class AdminUninstallSlackWorkspaceTests
{
    private readonly ISlackTeamRepository _repository = A.Fake<ISlackTeamRepository>();
    private readonly AdminUninstallSlackWorkspace _sut;

    public AdminUninstallSlackWorkspaceTests()
    {
        _sut = new AdminUninstallSlackWorkspace(_repository);
    }

    [Fact]
    public async Task Execute_UnknownTeam_ReturnsNullAndDoesNotDelete()
    {
        A.CallTo(() => _repository.FindByTeamId("T1")).Returns((SlackTeam?)null);

        var result = await _sut.Execute("T1");

        Assert.Null(result);
        A.CallTo(() => _repository.DeleteByTeamId(A<string>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task Execute_KnownTeam_DeletesAndReturnsWorkspace()
    {
        var team = new SlackTeam { TeamId = "T1", TeamName = "Team One", AccessToken = "token1" };
        A.CallTo(() => _repository.FindByTeamId("T1")).Returns(team);

        var result = await _sut.Execute("T1");

        Assert.NotNull(result);
        Assert.Equal("T1", result.TeamId);
        A.CallTo(() => _repository.DeleteByTeamId("T1")).MustHaveHappenedOnceExactly();
    }
}
