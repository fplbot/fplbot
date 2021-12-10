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

    private LineupState CreateNewLineupScenario()
    {
        var fixtureClient = A.Fake<IFixtureClient>();
        var testFixture1 = TestBuilder.NoGoals(1);
        var testFixture2 = TestBuilder.NoGoals(2);
        A.CallTo(() => fixtureClient.GetFixturesByGameweek(1)).Returns(new List<Fixture>
        {
            testFixture1,
            testFixture2
        });

        var scraperFake = A.Fake<IGetMatchDetails>();
        A.CallTo(() => scraperFake.GetMatchDetails(testFixture1.PulseId)).Returns(TestBuilder.NoLineup(testFixture1.PulseId));
        A.CallTo(() => scraperFake.GetMatchDetails(testFixture2.PulseId)).Returns(TestBuilder.NoLineup(testFixture2.PulseId)).Once().Then.Returns(TestBuilder.Lineup(testFixture2.PulseId));
        _session = new TestableMessageSession();
        return new LineupState(fixtureClient, scraperFake, _session, A.Fake<ILogger<LineupState>>());
    }

    private LineupState CreateTwoNewLineupsScenario()
    {
        var fixtureClient = A.Fake<IFixtureClient>();
        var testFixture1 = TestBuilder.NoGoals(1);
        var testFixture2 = TestBuilder.NoGoals(2);
        A.CallTo(() => fixtureClient.GetFixturesByGameweek(1)).Returns(new List<Fixture>
        {
            testFixture1,
            testFixture2
        });

        var scraperFake = A.Fake<IGetMatchDetails>();
        A.CallTo(() => scraperFake.GetMatchDetails(testFixture1.PulseId)).Returns(TestBuilder.NoLineup(testFixture1.PulseId)).Once().Then.Returns(TestBuilder.Lineup(testFixture1.PulseId));;
        A.CallTo(() => scraperFake.GetMatchDetails(testFixture2.PulseId)).Returns(TestBuilder.NoLineup(testFixture2.PulseId)).Once().Then.Returns(TestBuilder.Lineup(testFixture2.PulseId));
        _session = new TestableMessageSession();
        return new LineupState(fixtureClient, scraperFake, _session, A.Fake<ILogger<LineupState>>());
    }
}
