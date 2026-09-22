using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.States;
using FplBot.Data.Slack;
using FplBot.Domain;
using FplBot.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FplBot.Tests.E2E.Slack.SlackSubscriptions;

[Collection("App")]
public class LikelyPriceChangeEventPublishingE2ETests(AppFixture fixture) : IAsyncLifetime
{
    private const string Channel = "#players";

    public async ValueTask InitializeAsync()
    {
        fixture.SlackCapture.Reset();
        await fixture.FlushRedisAsync();
        var installation = SlackInstallationFaker.Generate();
        installation.Subscribe(Channel, [FplEvent.LikelyPriceChanges]);
        await fixture.Services.GetRequiredService<ISlackTeamRepository>().Save(installation);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task WithLikelyPriceChange_FiresOnceOnBecomingVeryLikely()
    {
        var state = CreateLikelyPriceChangeScenario();
        await state.Tick(CancellationToken.None); // init: seeds baseline
        await state.Tick(CancellationToken.None); // becomes very likely: notifies

        var msg = await fixture.SlackCapture.WaitForMessageAsync(Channel);
        Assert.Equal(Channel, msg.Channel);
        Assert.Contains("PlayerWebname", msg.Text);
    }

    [Fact]
    public async Task WithLikelyPriceChange_DoesNotRepeatWhileStillVeryLikely()
    {
        var state = CreateLikelyPriceChangeScenario();
        await state.Tick(CancellationToken.None); // init
        await state.Tick(CancellationToken.None); // becomes very likely: notifies
        await fixture.SlackCapture.WaitForMessageAsync(Channel);

        fixture.SlackCapture.Reset();
        await state.Tick(CancellationToken.None); // still very likely on the next poll: should NOT notify again

        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.SlackCapture.AnyMessage());
    }

    [Fact]
    public async Task WithLikelyPriceChange_RenotifiesAfterMomentumResetsToNeutral()
    {
        var state = CreateRearmingLikelyPriceChangeScenario();
        await state.Tick(CancellationToken.None); // init: seeds baseline
        await state.Tick(CancellationToken.None); // becomes very likely: notifies

        var first = await fixture.SlackCapture.WaitForMessageAsync(Channel);
        Assert.Contains("PlayerWebname", first.Text);

        fixture.SlackCapture.Reset();
        await state.Tick(CancellationToken.None); // momentum resets to neutral: should NOT notify

        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.SlackCapture.AnyMessage());

        await state.Tick(CancellationToken.None); // becomes very likely again: notifies once more

        var second = await fixture.SlackCapture.WaitForMessageAsync(Channel);
        Assert.Contains("PlayerWebname", second.Text);
    }

    private PlayerUpdatesMonitor CreateLikelyPriceChangeScenario()
    {
        var settingsClient = GlobalSettingsClientBuilder.Returning(
            new GlobalSettings
            {
                Teams = [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()],
                Players = [TestBuilder.Player()]
            },
            new GlobalSettings
            {
                Teams = [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()],
                Players = [TestBuilder.Player().WithNextPriceChangeLikelihood(5)]
            });

        return CreatePlayerBaseScenario(settingsClient);
    }

    private PlayerUpdatesMonitor CreateRearmingLikelyPriceChangeScenario()
    {
        var baseline = new GlobalSettings
        {
            Teams = [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()],
            Players = [TestBuilder.Player()]
        };
        var veryLikely = new GlobalSettings
        {
            Teams = [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()],
            Players = [TestBuilder.Player().WithNextPriceChangeLikelihood(5)]
        };
        var neutral = new GlobalSettings
        {
            Teams = [TestBuilder.HomeTeam(), TestBuilder.AwayTeam()],
            Players = [TestBuilder.Player().WithNextPriceChangeLikelihood(0)]
        };

        // PlayerUpdatesMonitor calls GetGlobalSettings() twice per non-init Process() call — once
        // to check whether it still holds initial state (discarded once state exists), once more for the
        // actual "after" snapshot used in the diff — so each logical poll after the first needs its value
        // returned twice in a row to keep both calls within that poll consistent.
        var fake = A.Fake<IGlobalSettingsClient>();
        A.CallTo(() => fake.GetGlobalSettings())
            .Returns(baseline).Once()
            .Then.Returns(veryLikely).Once()
            .Then.Returns(veryLikely).Once()
            .Then.Returns(neutral).Once()
            .Then.Returns(neutral).Once()
            .Then.Returns(veryLikely).Once()
            .Then.Returns(veryLikely);

        return CreatePlayerBaseScenario(fake);
    }

    private PlayerUpdatesMonitor CreatePlayerBaseScenario(IGlobalSettingsClient playerClient) =>
        new(playerClient, fixture.Services.GetRequiredService<IServiceScopeFactory>(), NullLogger<PlayerUpdatesMonitor>.Instance);
}
