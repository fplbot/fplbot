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
public class GameweekEventPublishingE2ETests(AppFixture fixture) : IAsyncLifetime
{
    private string _teamId = null!;
    private string _channel = null!;

    public async ValueTask InitializeAsync()
    {
        fixture.SlackCapture.Reset();
        await fixture.FlushRedisAsync();
        _channel = "#gameweeks-" + Guid.NewGuid().ToString("N")[..8];
        _teamId = await fixture.InstallSlackbot();
        await fixture.Subscribe(_teamId, _channel, FplEvent.Captains, FplEvent.Standings);

        // GameweekJustBegan/GameweekFinished only post to Slack for channels following a league —
        // drive that through the real "follow" command, same as FplChangeLeagueIdHandlerTests.
        fixture.SlackCapture.Reset();
        await fixture.AskSlackbot(_teamId, _channel, "<@UREFQD887> follow 12345");
        await fixture.SlackCapture.WaitForMessageAsync(_channel); // "Thanks! You're now following..."
        fixture.SlackCapture.Reset();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task OnFirstProcess_NoCurrentGameweekNoNextGameweek_DoesNothing()
    {
        var action = BuildMonitor(GlobalSettingsClientBuilder.Returning(GlobalSettingsWithGameweeks([])));

        await action.EveryOtherMinuteTick(CancellationToken.None);
        await action.EveryOtherMinuteTick(CancellationToken.None);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(_channel, TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task OnGameweekTransition_PublishesGameweekJustBegan()
    {
        var gameweekClient = GlobalSettingsClientBuilder.Returning(GameweeksBeforeTransition(), GameweeksAfterTransition());
        var action = BuildMonitor(gameweekClient);

        await action.EveryOtherMinuteTick(CancellationToken.None);
        await action.EveryOtherMinuteTick(CancellationToken.None);

        var msg = await fixture.SlackCapture.WaitForMessageAsync(_channel);
        Assert.Contains("Gameweek 3", msg.Text);
    }

    [Fact]
    public async Task OnGameweekFinished_PublishesGameweekFinished()
    {
        var gameweekClient = GlobalSettingsClientBuilder.Returning(GameweeksBeforeTransition(), GameweeksWithCurrentNowMarkedAsFinished());
        var action = BuildMonitor(gameweekClient);

        await action.EveryOtherMinuteTick(CancellationToken.None);
        await action.EveryOtherMinuteTick(CancellationToken.None);

        // GameweekFinished resolves the gameweek via the shared (bootstrap-static.json-backed)
        // IGlobalSettingsClient, and doesn't format any per-entry standings when the followed
        // league has no entries — it still confirms the finish by posting the intro text.
        var msg = await fixture.SlackCapture.WaitForMessageAsync(_channel);
        Assert.Contains("Gameweek 2", msg.Text);
    }

    [Fact]
    public async Task OnNoChanges_NoMassTransitEventsPublished()
    {
        var gameweekClient = GlobalSettingsClientBuilder.Returning(GameweeksWithCurrentNowMarkedAsFinished());
        var action = BuildMonitor(gameweekClient);

        await action.EveryOtherMinuteTick(CancellationToken.None);
        await action.EveryOtherMinuteTick(CancellationToken.None);
        await action.EveryOtherMinuteTick(CancellationToken.None);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.SlackCapture.WaitForMessageAsync(_channel, TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task FromPreseason_ToGw1_PublishesGw1Start()
    {
        var gameweekClient = GlobalSettingsClientBuilder.Returning(GlobalSettingsWithGameweeks(Preseason()), GlobalSettingsWithGameweeks(Gw1Current()));
        var action = BuildMonitor(gameweekClient);

        await action.EveryOtherMinuteTick(CancellationToken.None);
        await action.EveryOtherMinuteTick(CancellationToken.None);

        var msg = await fixture.SlackCapture.WaitForMessageAsync(_channel);
        Assert.Contains("Gameweek 1", msg.Text);
    }

    private GameweekLifecycleMonitor BuildMonitor(IGlobalSettingsClient gameweekClient) =>
        new(gameweekClient,
            A.Fake<ILogger<GameweekLifecycleMonitor>>(),
            fixture.Services.GetRequiredService<IServiceScopeFactory>(),
            A.Fake<IFixtureState>(),
            A.Fake<ILineupState>());

    private List<Gameweek> Preseason() =>
    [
        new() { Id = 1, IsCurrent = false, IsNext = true },
        new() { Id = 2 }
    ];

    private List<Gameweek> Gw1Current() =>
    [
        TestBuilder.CurrentGameweek(1),
        TestBuilder.NextGameweek(2)
    ];

    private static List<Gameweek> SomeGameweeks() =>
    [
        TestBuilder.PreviousGameweek(1),
        TestBuilder.CurrentGameweek(2),
        TestBuilder.NextGameweek(3)
    ];

    private static GlobalSettings GameweeksBeforeTransition() => GlobalSettingsWithGameweeks(SomeGameweeks());

    private static GlobalSettings GameweeksAfterTransition() => GlobalSettingsWithGameweeks([
        TestBuilder.OlderGameweek(1),
        TestBuilder.PreviousGameweek(2),
        TestBuilder.CurrentGameweek(3)
    ]);

    private static GlobalSettings GameweeksWithCurrentNowMarkedAsFinished()
    {
        var currentGameweek = TestBuilder.CurrentGameweek(2);
        currentGameweek.IsFinished = true;
        return GlobalSettingsWithGameweeks([
            TestBuilder.PreviousGameweek(1),
            currentGameweek,
            TestBuilder.NextGameweek(3)
        ]);
    }

    private static GlobalSettings GlobalSettingsWithGameweeks(List<Gameweek> gameweeks) =>
        new() { Gameweeks = gameweeks };
}
