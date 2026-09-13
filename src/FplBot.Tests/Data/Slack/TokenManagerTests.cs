using FakeItEasy;
using FplBot.Data;
using FplBot.Data.Slack;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Services.WebApi.Slack.Handlers.Reactors;
using FplBot.Tests.Helpers;
using Slackbot.Net.Abstractions.Hosting;

namespace FplBot.Tests.Data.Slack;

public class TokenManagerTests
{
    private readonly ISlackTeamRepository _repository = A.Fake<ISlackTeamRepository>();
    private readonly TestPublishEndpoint _publishEndpoint = new();
    private readonly TokenManager _sut;

    public TokenManagerTests()
    {
        _sut = new TokenManager(_repository, new TestScopeFactory(_publishEndpoint));
    }

    [Fact]
    public async Task Insert_Workspace_SavesBareInstallationAndPublishesAppInstalled()
    {
        await _sut.Insert(new Workspace("T1", "Team One", "token1"));

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
    public async Task Insert_SlackTeam_JustSavesWithoutPublishing()
    {
        var team = new SlackTeam { TeamId = "T1", TeamName = "Team One", AccessToken = "token1" };

        await _sut.Insert(team);

        A.CallTo(() => _repository.Save(team)).MustHaveHappenedOnceExactly();
        Assert.Empty(_publishEndpoint.PublishedMessages);
    }

    [Fact]
    public async Task Delete_UnknownTeam_ReturnsNullAndDoesNotDelete()
    {
        A.CallTo(() => _repository.FindByTeamId("T1")).Returns((SlackTeam?)null);

        var result = await _sut.Delete("T1");

        Assert.Null(result);
        A.CallTo(() => _repository.DeleteByTeamId(A<string>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task Delete_KnownTeam_DeletesAndReturnsOriginalWorkspace()
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

        var result = await _sut.Delete("T1");

        Assert.NotNull(result);
        Assert.Equal("T1", result.TeamId);
        Assert.Equal("Team One", result.TeamName);
        Assert.Equal("token1", result.Token);
        A.CallTo(() => _repository.DeleteByTeamId("T1")).MustHaveHappenedOnceExactly();
    }
}
