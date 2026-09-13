using FakeItEasy;
using FplBot.ApplicationServices.Slack;
using FplBot.Data.Slack;
using FplBot.Domain;
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
        _sut = new AdminUninstallSlackWorkspace(_repository, _publishEndpoint);
    }

    [Fact]
    public async Task Execute_KnownTeam_MarksForRemovalSavesAndPublishes()
    {
        var installation = SlackInstallation.Install("T1", "Team One", "token1");
        A.CallTo(() => _repository.GetInstallation("T1")).Returns(installation);

        await _sut.Execute("T1");

        A.CallTo(() => _repository.Save(A<SlackTeam>.That.Matches(t => t.TeamId == "T1" && t.PendingRemoval == true)))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _repository.DeleteByTeamId(A<string>._)).MustNotHaveHappened();

        var published = Assert.Single(_publishEndpoint.PublishedMessages.Containing<TeamMarkedForRemoval>());
        var evt = Assert.IsType<TeamMarkedForRemoval>(published.Message);
        Assert.Equal("T1", evt.TeamId);
    }
}
