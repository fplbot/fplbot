using FakeItEasy;
using FplBot.ApplicationServices.Slack;
using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Services.WebApi.Slack.Handlers.Reactors;
using FplBot.Tests.Helpers;
using Slackbot.Net.Abstractions.Hosting;

namespace FplBot.Tests.Data.Slack;

public class WorkspaceInstallationHandlerTests
{
    private readonly ISlackTeamRepository _repository = A.Fake<ISlackTeamRepository>();
    private readonly TestPublishEndpoint _publishEndpoint = new();
    private readonly WorkspaceInstallationHandler _sut;

    public WorkspaceInstallationHandlerTests()
    {
        var scopeFactory = new TestScopeFactory(_publishEndpoint);
        _sut = new WorkspaceInstallationHandler(_repository, scopeFactory, new UninstallSlackWorkspace(_repository, scopeFactory));
    }

    [Fact]
    public async Task Install_SavesBareInstallationAndPublishesAppInstalled()
    {
        await _sut.Install(new Workspace("T1", "Team One", "token1"));

        A.CallTo(() => _repository.Save(A<SlackTeam>.That.Matches(t =>
            t.TeamId == "T1" && t.TeamName == "Team One" && t.AccessToken == "token1" &&
            t.FplBotSlackChannel == null && t.FplbotLeagueId == null)))
            .MustHaveHappenedOnceExactly();

        var published = Assert.Single(_publishEndpoint.PublishedMessages.Containing<AppInstalled>());
        var appInstalled = Assert.IsType<AppInstalled>(published.Message);
        Assert.Equal("T1", appInstalled.TeamId);
        Assert.Equal("Team One", appInstalled.TeamName);
        Assert.Equal(ChatPlatform.Slack, appInstalled.Platform);
    }

    [Fact]
    public async Task Uninstall_DelegatesToUninstallSlackWorkspace()
    {
        var team = new SlackTeam { TeamId = "T1", TeamName = "Team One", AccessToken = "token1" };
        A.CallTo(() => _repository.FindByTeamId("T1")).Returns(team);

        var result = await _sut.Uninstall("T1");

        Assert.NotNull(result);
        Assert.Equal("T1", result.TeamId);
        A.CallTo(() => _repository.DeleteByTeamId("T1")).MustHaveHappenedOnceExactly();
        Assert.Single(_publishEndpoint.PublishedMessages.Containing<AppUninstalled>());
    }
}
