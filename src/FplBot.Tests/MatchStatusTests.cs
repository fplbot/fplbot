using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.Abstractions;
using Fpl.EventPublishers.States;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;

namespace FplBot.Tests;

public class MatchStatusTests
{
    private TestableMessageSession _session;

    [Fact]
    public async Task DoesNotEmitInInitPhase()
    {
        var monitor = CreateNewLineupScenario();
        await monitor.Reset(1);

        Assert.Empty(_session.PublishedMessages);
    }

    [Fact]
    public async Task WhenLineupsInAFixture_EmitsEvent()
    {
        var monitor = CreateNewLineupScenario();
        await monitor.Reset(1);
        await monitor.Refresh(1);

        Assert.Single(_session.PublishedMessages);
        Assert.IsType<LineupReady>(_session.PublishedMessages[0].Message);
    }

    [Fact]
    public async Task WhenLineupsInSingleFixture_SequencialRefreshes_EmitsEventOnlyOnce()
    {
        var monitor = CreateNewLineupScenario();

        await monitor.Reset(1);

        await monitor.Refresh(1);
        await monitor.Refresh(1);

        Assert.Single(_session.PublishedMessages);
        Assert.IsType<LineupReady>(_session.PublishedMessages[0].Message);
    }

    [Fact]
    public async Task WhenLineupsInTwoFixtures_SequencialRefreshes_EmitsOneEventPrFixture()
    {
        var monitor = CreateTwoNewLineupsScenario();
        await monitor.Reset(1);
        await monitor.Refresh(1);

        Assert.Equal(2, _session.PublishedMessages.Length);
        Assert.IsType<LineupReady>(_session.PublishedMessages[0].Message);
    }

    [Fact]
    public async Task WhenFixtureIsRemoved_EmitsFixtureRemoved()
    {
        var monitor = CreateFixture2RemovedScenario();
        await monitor.Reset(1);
        await monitor.Refresh(1);

        Assert.Single(_session.PublishedMessages);
        var message = _session.PublishedMessages[0].Message;
        Assert.IsType<FixtureRemovedFromGameweek>(message);
        var fixtureRemovedFromGameweekEvent = ((FixtureRemovedFromGameweek)message);
        Assert.Equal(1, fixtureRemovedFromGameweekEvent.Gameweek);
        Assert.Equal(new RemovedFixture(2, new(10,"HomeTeam","HOM"), new(20, "AwAyTeam", "AWA")), fixtureRemovedFromGameweekEvent.RemovedFixture);
    }

    private LineupState CreateNewLineupScenario()
    {
        var fixtureClient = A.Fake<IFixtureClient>();
        var testFixture1 = TestBuilder.NoGoals(1).NotStarted();
        var testFixture2 = TestBuilder.NoGoals(2).NotStarted();
        A.CallTo(() => fixtureClient.GetFixturesByGameweek(1)).Returns(new List<Fixture>
        {
            testFixture1,
            testFixture2
        });

        var pulseFake = A.Fake<IPulseLiveClient>();
        A.CallTo(() => pulseFake.GetMatchDetails(testFixture1.PulseId)).Returns(TestBuilder.NoLineup(testFixture1.PulseId));
        A.CallTo(() => pulseFake.GetMatchDetails(testFixture2.PulseId)).Returns(TestBuilder.NoLineup(testFixture2.PulseId)).Once().Then.Returns(TestBuilder.Lineup(testFixture2.PulseId));
        _session = new TestableMessageSession();
        return new LineupState(fixtureClient, pulseFake, A.Fake<IGlobalSettingsClient>(), _session, A.Fake<ILogger<LineupState>>());
    }

    private LineupState CreateTwoNewLineupsScenario()
    {
        var fixtureClient = A.Fake<IFixtureClient>();
        var testFixture1 = TestBuilder.NoGoals(1).NotStarted();
        var testFixture2 = TestBuilder.NoGoals(2).NotStarted();
        A.CallTo(() => fixtureClient.GetFixturesByGameweek(1)).Returns(new List<Fixture>
        {
            testFixture1,
            testFixture2
        });

        var pulseClient = A.Fake<IPulseLiveClient>();
        A.CallTo(() => pulseClient.GetMatchDetails(testFixture1.PulseId)).Returns(TestBuilder.NoLineup(testFixture1.PulseId)).Once().Then.Returns(TestBuilder.Lineup(testFixture1.PulseId));;
        A.CallTo(() => pulseClient.GetMatchDetails(testFixture2.PulseId)).Returns(TestBuilder.NoLineup(testFixture2.PulseId)).Once().Then.Returns(TestBuilder.Lineup(testFixture2.PulseId));
        _session = new TestableMessageSession();
        return new LineupState(fixtureClient, pulseClient, A.Fake<IGlobalSettingsClient>(), _session, A.Fake<ILogger<LineupState>>());
    }

    private LineupState CreateFixture2RemovedScenario()
    {
        var fixtureClient = A.Fake<IFixtureClient>();
        A.CallTo(() => fixtureClient.GetFixturesByGameweek(1)).Returns(new List<Fixture>
        {
            TestBuilder.NoGoals(1),
            TestBuilder.NoGoals(2)
        }).Once().Then.Returns(new List<Fixture>()
        {
            TestBuilder.NoGoals(1)
        });

        var pulseClient = A.Fake<IPulseLiveClient>();
       _session = new TestableMessageSession();
       var globalSettingsClient = A.Fake<IGlobalSettingsClient>();
       A.CallTo(() => globalSettingsClient.GetGlobalSettings()).Returns(new GlobalSettings
           {
               Teams = new List<Team> { TestBuilder.HomeTeam(), TestBuilder.AwayTeam() },
               Players = new List<Player> { TestBuilder.Player().WithStatus(PlayerStatuses.Available) }
           }
       );
        return new LineupState(fixtureClient, pulseClient, globalSettingsClient, _session, A.Fake<ILogger<LineupState>>());
    }
}
