using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.States;
using FplBot.Domain;
using FplBot.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FplBot.Tests.E2E.Slack.SlackSubscriptions;

[Collection("App")]
public class FixtureEventE2ETests(AppFixture fixture) : IAsyncLifetime
{
    private string _channel = null!;

    public async ValueTask InitializeAsync()
    {
        fixture.SlackCapture.Reset();
        await fixture.FlushRedisAsync();
        _channel = "#fixtures-" + Guid.NewGuid().ToString("N")[..8];
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, _channel, FplEvent.FixtureGoals, FplEvent.FixtureFullTime);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task DoesNotCrashWithNoDataReturned()
    {
        var state = CreateFixtureState(A.Fake<IFixtureClient>(), A.Fake<IGlobalSettingsClient>());

        await state.Reset(1);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(_channel, TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task WithNoChangeInFixtures_DoesNotEmitEvent()
    {
        var fixtureClient = A.Fake<IFixtureClient>();
        A.CallTo(() => fixtureClient.GetFixturesByGameweek(1)).Returns(new List<Fixture> { TestBuilder.NoGoals(1) });
        var settingsClient = GlobalSettingsClientBuilder.Returning(new GlobalSettings
        {
            Teams = new List<Team> { TestBuilder.HomeTeam(), TestBuilder.AwayTeam() },
            Players = new List<Player> { TestBuilder.Player() }
        });
        var state = CreateFixtureState(fixtureClient, settingsClient);

        await state.Reset(1);
        await state.Refresh(1);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(_channel, TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task WithGoalScoredEvent()
    {
        var state = CreateGoalScoredScenario();

        await state.Reset(1);
        await state.Refresh(1);

        var msg = await fixture.SlackCapture.WaitForMessageAsync(_channel);
        Assert.Contains("PlayerWebname", msg.Text);
    }

    [Fact]
    public async Task WithSingleProvisionalFinished_EmitsEvent()
    {
        const int fixtureId = 888;
        SeedMatchingFinishedFixtureLookup(fixtureId);
        var state = CreateSingleFinishedFixturesScenario(fixtureId);

        await state.Reset(1);
        await state.Refresh(1);

        var msg = await fixture.SlackCapture.WaitForMessageAsync(_channel);
        Assert.Contains("FT:", msg.Text);
    }

    [Fact]
    public async Task WithMultipleProvisionalFinished_EmitsEventPerFixture()
    {
        const int fixtureId1 = 888;
        const int fixtureId2 = 999;
        SeedMatchingFinishedFixtureLookup(fixtureId1);
        SeedMatchingFinishedFixtureLookup(fixtureId2);
        var state = CreateMultipleFinishedFixturesScenario(fixtureId1, fixtureId2);

        await state.Reset(1);
        await state.Refresh(1);

        var first = await fixture.SlackCapture.WaitForMessageAsync(_channel);
        var second = await fixture.SlackCapture.WaitForMessageAsync(_channel);
        Assert.Contains("FT:", first.Text);
        Assert.Contains("FT:", second.Text);
    }

    [Fact]
    public async Task GoalScored_ChannelNotSubscribedToGoals_NoSlackMessage()
    {
        var otherTeamId = await fixture.InstallSlackbot();
        var otherChannel = "#fixtures-" + Guid.NewGuid().ToString("N")[..8];
        await fixture.Subscribe(otherTeamId, otherChannel, FplEvent.FixtureFullTime);

        var state = CreateGoalScoredScenario();
        await state.Reset(1);
        await state.Refresh(1);

        var msg = await fixture.SlackCapture.WaitForMessageAsync(_channel);
        Assert.Contains("PlayerWebname", msg.Text);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(otherChannel, TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task FixtureFinished_ChannelNotSubscribedToFullTime_NoSlackMessage()
    {
        var otherTeamId = await fixture.InstallSlackbot();
        var otherChannel = "#fixtures-" + Guid.NewGuid().ToString("N")[..8];
        await fixture.Subscribe(otherTeamId, otherChannel, FplEvent.FixtureGoals);

        const int fixtureId = 777;
        SeedMatchingFinishedFixtureLookup(fixtureId);
        var state = CreateSingleFinishedFixturesScenario(fixtureId);

        await state.Reset(1);
        await state.Refresh(1);

        var msg = await fixture.SlackCapture.WaitForMessageAsync(_channel);
        Assert.Contains("FT:", msg.Text);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(otherChannel, TimeSpan.FromMilliseconds(500)));
    }

    // SlackFixtureFulltimeHandler looks the finished fixture back up via the shared IFixtureClient
    // (not the one FixtureState was constructed with), and resolves teams via the shared
    // IGlobalSettingsClient — so it needs real FPL team ids (Arsenal=1, Aston Villa=2) from
    // bootstrap-static.json, not TestBuilder's synthetic teams.
    private void SeedMatchingFinishedFixtureLookup(int fixtureId)
    {
        _knownFinishedFixtures.Add(new Fixture
        {
            Id = fixtureId,
            Event = 1,
            HomeTeamId = 1,
            AwayTeamId = 2,
            HomeTeamScore = 1,
            AwayTeamScore = 0
        });

        var sharedFixtureClient = fixture.Services.GetRequiredService<IFixtureClient>();
        A.CallTo(() => sharedFixtureClient.GetFixtures()).ReturnsLazily(() => _knownFinishedFixtures.ToList());
    }

    private readonly List<Fixture> _knownFinishedFixtures = new();

    private FixtureState CreateFixtureState(IFixtureClient fixtureClient, IGlobalSettingsClient settingsClient) =>
        new(fixtureClient, settingsClient, fixture.Services.GetRequiredService<IServiceScopeFactory>(), A.Fake<ILogger<FixtureState>>());

    private FixtureState CreateGoalScoredScenario()
    {
        var playerClient = GlobalSettingsClientBuilder.Returning(
            new GlobalSettings
            {
                Teams = new List<Team> { TestBuilder.HomeTeam(), TestBuilder.AwayTeam() },
                Players = new List<Player> { TestBuilder.Player() }
            });

        var fixtureClient = A.Fake<IFixtureClient>();
        A.CallTo(() => fixtureClient.GetFixturesByGameweek(1)).Returns(new List<Fixture>
            {
                TestBuilder.AwayTeamGoal(888, 1)
            }).Once()
            .Then.Returns(new List<Fixture>
            {
                TestBuilder.AwayTeamGoal(888, 2)
            });

        return CreateFixtureState(fixtureClient, playerClient);
    }

    private FixtureState CreateSingleFinishedFixturesScenario(int fixtureId)
    {
        var playerClient = GlobalSettingsClientBuilder.Returning(
            new GlobalSettings
            {
                Teams = new List<Team> { TestBuilder.HomeTeam(), TestBuilder.AwayTeam() },
                Players = new List<Player> { TestBuilder.Player(), TestBuilder.OtherPlayer() }
            });

        var fixtureClient = A.Fake<IFixtureClient>();
        A.CallTo(() => fixtureClient.GetFixturesByGameweek(1)).Returns(new List<Fixture>
            {
                TestBuilder.AwayTeamGoal(fixtureId, 1)
            }).Once()
            .Then.Returns(new List<Fixture>
            {
                TestBuilder.AwayTeamGoal(fixtureId, 1).FinishedProvisional()
            });

        return CreateFixtureState(fixtureClient, playerClient);
    }

    private FixtureState CreateMultipleFinishedFixturesScenario(int fixtureId1, int fixtureId2)
    {
        var playerClient = GlobalSettingsClientBuilder.Returning(
            new GlobalSettings
            {
                Teams = new List<Team> { TestBuilder.HomeTeam(), TestBuilder.AwayTeam() },
                Players = new List<Player> { TestBuilder.Player() }
            });

        var fixtureClient = A.Fake<IFixtureClient>();
        A.CallTo(() => fixtureClient.GetFixturesByGameweek(1)).Returns(new List<Fixture>
            {
                TestBuilder.AwayTeamGoal(fixtureId1, 1),
                TestBuilder.AwayTeamGoal(fixtureId2, 1)
            }).Once()
            .Then.Returns(new List<Fixture>
            {
                TestBuilder.AwayTeamGoal(fixtureId1, 1).FinishedProvisional(),
                TestBuilder.AwayTeamGoal(fixtureId2, 1).FinishedProvisional()
            });

        return CreateFixtureState(fixtureClient, playerClient);
    }
}
