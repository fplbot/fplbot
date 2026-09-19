using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.States;
using Fpl.PulseLive;
using FplBot.Domain;
using FplBot.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FplBot.Tests.E2E.Slack.SlackSubscriptions;

[Collection("App")]
public class LineupEventPublishingE2ETests(AppFixture fixture) : IAsyncLifetime
{
    private string _channel = null!;

    public async ValueTask InitializeAsync()
    {
        fixture.SlackCapture.Reset();
        await fixture.FlushRedisAsync();
        _channel = "#lineups-" + Guid.NewGuid().ToString("N")[..8];
        var teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(teamId, _channel, FplEvent.Lineups, FplEvent.FixtureRemovedFromGameweek);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task DoesNotEmitInInitPhase()
    {
        var monitor = CreateNewLineupScenario();

        await monitor.Reset(1);

        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.SlackCapture.AnyMessage(_channel));
    }

    [Fact]
    public async Task WhenLineupsInAFixture_EmitsEvent()
    {
        var monitor = CreateNewLineupScenario();
        await monitor.Reset(1);

        await monitor.Refresh(1);

        var msg = await fixture.SlackCapture.WaitForMessageAsync(_channel);
        Assert.Contains("Lineups", msg.Text);
    }

    [Fact]
    public async Task WhenLineupsInSingleFixture_SequencialRefreshes_EmitsEventOnlyOnce()
    {
        var monitor = CreateNewLineupScenario();
        await monitor.Reset(1);

        await monitor.Refresh(1);
        await monitor.Refresh(1);

        // One LineupReady produces two Slack messages: the main post and its threaded lineup reply.
        await fixture.SlackCapture.WaitForMessageAsync(_channel);
        await fixture.SlackCapture.WaitForMessageAsync(_channel);
        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.SlackCapture.AnyMessage(_channel));
    }

    [Fact]
    public async Task WhenLineupsInTwoFixtures_SequencialRefreshes_EmitsOneEventPrFixture()
    {
        var monitor = CreateTwoNewLineupsScenario();
        await monitor.Reset(1);

        await monitor.Refresh(1);

        // Two LineupReady events, each producing a main post + threaded reply.
        await fixture.SlackCapture.WaitForMessageAsync(_channel);
        await fixture.SlackCapture.WaitForMessageAsync(_channel);
        await fixture.SlackCapture.WaitForMessageAsync(_channel);
        await fixture.SlackCapture.WaitForMessageAsync(_channel);
    }

    [Fact]
    public async Task WhenFixtureIsRemoved_EmitsFixtureRemoved()
    {
        var monitor = CreateFixture2RemovedScenario();
        await monitor.Reset(1);

        await monitor.Refresh(1);

        var msg = await fixture.SlackCapture.WaitForMessageAsync(_channel);
        Assert.Contains("Fixture off!", msg.Text);
        Assert.Contains("HomeTeam-AwAyTeam", msg.Text);
    }

    private LineupState CreateNewLineupScenario()
    {
        var fixtureClient = A.Fake<IFixtureClient>();
        var testFixture1 = TestBuilder.NoGoals(1).NotStarted();
        var testFixture2 = TestBuilder.NoGoals(2).NotStarted();
        A.CallTo(() => fixtureClient.GetFixturesByGameweek(1)).Returns(
        [
            testFixture1,
            testFixture2
        ]);

        var pulseFake = A.Fake<IPulseLiveClient>();
        A.CallTo(() => pulseFake.GetMatchDetails(testFixture1.Code)).Returns(TestBuilder.NoLineup(testFixture1.Code));
        A.CallTo(() => pulseFake.GetMatchDetails(testFixture2.Code)).Returns(TestBuilder.NoLineup(testFixture2.Code)).Once().Then
            .Returns(TestBuilder.Lineup(testFixture2.Code));
        var globalSettingsClient = GlobalSettingsClientBuilder.Returning(new GlobalSettings { Teams = [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()] });
        return CreateLineupState(fixtureClient, pulseFake, globalSettingsClient);
    }

    private LineupState CreateTwoNewLineupsScenario()
    {
        var fixtureClient = A.Fake<IFixtureClient>();
        var testFixture1 = TestBuilder.NoGoals(1).NotStarted();
        var testFixture2 = TestBuilder.NoGoals(2).NotStarted();
        A.CallTo(() => fixtureClient.GetFixturesByGameweek(1)).Returns(
        [
            testFixture1,
            testFixture2
        ]);

        var pulseClient = A.Fake<IPulseLiveClient>();
        A.CallTo(() => pulseClient.GetMatchDetails(testFixture1.Code)).Returns(TestBuilder.NoLineup(testFixture1.Code)).Once().Then
            .Returns(TestBuilder.Lineup(testFixture1.Code));
        A.CallTo(() => pulseClient.GetMatchDetails(testFixture2.Code)).Returns(TestBuilder.NoLineup(testFixture2.Code)).Once().Then
            .Returns(TestBuilder.Lineup(testFixture2.Code));
        var globalSettingsClient = GlobalSettingsClientBuilder.Returning(new GlobalSettings { Teams = [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()] });
        return CreateLineupState(fixtureClient, pulseClient, globalSettingsClient);
    }

    private LineupState CreateFixture2RemovedScenario()
    {
        var fixtureClient = A.Fake<IFixtureClient>();
        A.CallTo(() => fixtureClient.GetFixturesByGameweek(1)).Returns(
        [
            TestBuilder.NoGoals(1),
            TestBuilder.NoGoals(2)
        ]).Once().Then.Returns(
        [
            TestBuilder.NoGoals(1)
        ]);

        var pulseClient = A.Fake<IPulseLiveClient>();
        var globalSettingsClient = GlobalSettingsClientBuilder.Returning(new GlobalSettings
        {
            Teams = [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()],
            Players = [TestBuilder.Player().WithStatus(PlayerStatuses.Available)]
        });
        return CreateLineupState(fixtureClient, pulseClient, globalSettingsClient);
    }

    private LineupState CreateLineupState(IFixtureClient fixtureClient, IPulseLiveClient pulseClient, IGlobalSettingsClient globalSettingsClient) =>
        new(fixtureClient, pulseClient, globalSettingsClient, fixture.Services.GetRequiredService<IServiceScopeFactory>(), A.Fake<ILogger<LineupState>>());
}
