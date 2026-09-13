using FakeItEasy;
using FplBot.Data.Slack;
using FplBot.Domain;
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

    private static SlackInstallation PendingRemovalInstallation()
    {
        var installation = SlackInstallation.Install("T1", "Team One", "token1");
        installation.MarkForRemoval();
        return installation;
    }

    [Fact]
    public async Task TeamNotFound_DoesNotCrashAndDoesNotCallSlack()
    {
        A.CallTo(() => _repository.FindInstallationByTeamId("T1")).Returns((SlackInstallation)null!);

        await _sut.Consume(BuildContext("T1"));

        A.CallTo(_slackClient).MustNotHaveHappened();
        A.CallTo(() => _repository.Delete(A<SlackInstallation>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task SlackAcceptsUninstall_DeletesLocally()
    {
        A.CallTo(() => _repository.FindInstallationByTeamId("T1")).Returns(PendingRemovalInstallation());
        A.CallTo(() => _slackClient.AppsUninstall("id", "secret")).Returns(new SlackResponse { Ok = true });

        await _sut.Consume(BuildContext("T1"));

        A.CallTo(() => _repository.Delete(A<SlackInstallation>.That.Matches(i => i.TeamId == "T1"))).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SlackRejectsUninstall_StillDeletesLocally()
    {
        A.CallTo(() => _repository.FindInstallationByTeamId("T1")).Returns(PendingRemovalInstallation());
        A.CallTo(() => _slackClient.AppsUninstall("id", "secret")).Returns(new SlackResponse { Ok = false, Error = "something_broke" });

        await _sut.Consume(BuildContext("T1"));

        A.CallTo(() => _repository.Delete(A<SlackInstallation>.That.Matches(i => i.TeamId == "T1"))).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task SlackThrows_StillDeletesLocallyAndDoesNotRethrow()
    {
        A.CallTo(() => _repository.FindInstallationByTeamId("T1")).Returns(PendingRemovalInstallation());
        A.CallTo(() => _slackClient.AppsUninstall("id", "secret"))
            .Throws(new WellKnownSlackApiException(error: "account_inactive", responseContent: "{}"));

        await _sut.Consume(BuildContext("T1"));

        A.CallTo(() => _repository.Delete(A<SlackInstallation>.That.Matches(i => i.TeamId == "T1"))).MustHaveHappenedOnceExactly();
    }

    private static ConsumeContext<TeamMarkedForRemoval> BuildContext(string teamId)
    {
        var context = A.Fake<ConsumeContext<TeamMarkedForRemoval>>();
        A.CallTo(() => context.Message).Returns(new TeamMarkedForRemoval(teamId));
        return context;
    }
}
