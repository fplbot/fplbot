using FakeItEasy;
using FplBot.Data.Slack;
using FplBot.EventHandlers;
using FplBot.Messaging.Contracts.Events.v1;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Slackbot.Net.Endpoints.Hosting;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Exceptions;
using SlackResponse = Slackbot.Net.SlackClients.Http.Models.Responses.Response;

namespace FplBot.Tests.EventHandlers;

public class TeamMarkedForRemovalHandlerTests
{
    private readonly ISlackTeamRepository _repository = A.Fake<ISlackTeamRepository>();
    private readonly ISlackClient _slackClient = A.Fake<ISlackClient>();
    private readonly ISlackClientBuilder _slackClientBuilder = A.Fake<ISlackClientBuilder>();
    private readonly TeamMarkedForRemovalHandler _sut;

    public TeamMarkedForRemovalHandlerTests()
    {
        A.CallTo(() => _slackClientBuilder.Build(A<string>._)).Returns(_slackClient);
        _sut = new TeamMarkedForRemovalHandler(
            _repository,
            _slackClientBuilder,
            Options.Create(new OAuthOptions { CLIENT_ID = "id", CLIENT_SECRET = "secret" }),
            NullLogger<TeamMarkedForRemovalHandler>.Instance);
    }

    [Fact]
    public async Task TeamNotFound_DoesNotCrashAndDoesNotCallSlack()
    {
        A.CallTo(() => _repository.GetTeam("T1")).Returns((SlackTeam)null!);

        await _sut.Consume(BuildContext("T1", "Team One"));

        A.CallTo(_slackClient).MustNotHaveHappened();
        A.CallTo(() => _repository.DeleteByTeamId(A<string>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task SlackAcceptsUninstall_DeletesLocally()
    {
        var team = new SlackTeam { TeamId = "T1", TeamName = "Team One", AccessToken = "token1" };
        A.CallTo(() => _repository.GetTeam("T1")).Returns(team);
        A.CallTo(() => _slackClient.AppsUninstall("id", "secret")).Returns(new SlackResponse { Ok = true });

        await _sut.Consume(BuildContext("T1", "Team One"));

        A.CallTo(() => _repository.DeleteByTeamId("T1")).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SlackRejectsUninstall_StillDeletesLocally()
    {
        var team = new SlackTeam { TeamId = "T1", TeamName = "Team One", AccessToken = "token1" };
        A.CallTo(() => _repository.GetTeam("T1")).Returns(team);
        A.CallTo(() => _slackClient.AppsUninstall("id", "secret")).Returns(new SlackResponse { Ok = false, Error = "something_broke" });

        await _sut.Consume(BuildContext("T1", "Team One"));

        A.CallTo(() => _repository.DeleteByTeamId("T1")).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SlackThrows_StillDeletesLocallyAndDoesNotRethrow()
    {
        var team = new SlackTeam { TeamId = "T1", TeamName = "Team One", AccessToken = "token1" };
        A.CallTo(() => _repository.GetTeam("T1")).Returns(team);
        A.CallTo(() => _slackClient.AppsUninstall("id", "secret"))
            .Throws(new WellKnownSlackApiException(error: "account_inactive", responseContent: "{}"));

        await _sut.Consume(BuildContext("T1", "Team One"));

        A.CallTo(() => _repository.DeleteByTeamId("T1")).MustHaveHappenedOnceExactly();
    }

    private static ConsumeContext<TeamMarkedForRemoval> BuildContext(string teamId, string teamName)
    {
        var context = A.Fake<ConsumeContext<TeamMarkedForRemoval>>();
        A.CallTo(() => context.Message).Returns(new TeamMarkedForRemoval(teamId, teamName));
        return context;
    }
}
