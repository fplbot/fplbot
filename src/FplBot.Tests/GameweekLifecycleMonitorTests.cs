using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.States;
using Microsoft.Extensions.Logging;
using FplBot.Tests.Helpers;

namespace FplBot.Tests;

public class GameweekLifecycleMonitorTests
{
    [Fact]
    public async Task OnFirstProcess_InitializesState()
    {
        var gameweekClient = A.Fake<IGlobalSettingsClient>();
        A.CallTo(() => gameweekClient.GetGlobalSettings()).Returns(GlobalSettingsWithGameweeks(SomeGameweeks()));

        var (action, fixtureState, lineupState, session) = BuildMonitor(gameweekClient);

        await action.EveryOtherMinuteTick(CancellationToken.None);

        A.CallTo(() => fixtureState.Reset(2)).MustHaveHappenedOnceExactly();
        A.CallTo(() => fixtureState.Refresh(A<int>._)).MustNotHaveHappened();
        Assert.Empty(session.PublishedMessages);
    }

    [Fact]
    public async Task OnFirstProcess_NoCurrentGameweekNoNextGameweek_DoesNothing()
    {
        var gameweekClient = A.Fake<IGlobalSettingsClient>();
        A.CallTo(() => gameweekClient.GetGlobalSettings()).Returns(GlobalSettingsWithGameweeks(new List<Gameweek>()));

        var (action, fixtureState, lineupState, session) = BuildMonitor(gameweekClient);

        await action.EveryOtherMinuteTick(CancellationToken.None);
        await action.EveryOtherMinuteTick(CancellationToken.None);

        A.CallTo(() => fixtureState.Reset(A<int>._)).MustNotHaveHappened();
        A.CallTo(() => fixtureState.Refresh(A<int>._)).MustNotHaveHappened();
        Assert.Empty(session.PublishedMessages);
    }

    [Fact]
    public async Task OnFirstProcessAndFollowing_InitializesAndRefreshes()
    {
        var gameweekClient = A.Fake<IGlobalSettingsClient>();
        A.CallTo(() => gameweekClient.GetGlobalSettings()).Returns(GlobalSettingsWithGameweeks(SomeGameweeks()));

        var (action, fixtureState, lineupState, session) = BuildMonitor(gameweekClient);

        await action.EveryOtherMinuteTick(CancellationToken.None);
        await action.EveryOtherMinuteTick(CancellationToken.None);

        A.CallTo(() => fixtureState.Reset(2)).MustHaveHappenedOnceExactly();
        A.CallTo(() => fixtureState.Refresh(2)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task OnGameweekTransition_PublishesMassTransitEventAndResetsState()
    {
        var gameweekClient = A.Fake<IGlobalSettingsClient>();
        A.CallTo(() => gameweekClient.GetGlobalSettings())
            .Returns(GameweeksBeforeTransition()).Once()
            .Then.Returns(GameweeksAfterTransition());

        var (action, fixtureState, lineupState, session) = BuildMonitor(gameweekClient);

        await action.EveryOtherMinuteTick(CancellationToken.None);
        await action.EveryOtherMinuteTick(CancellationToken.None);

        A.CallTo(() => fixtureState.Reset(2)).MustHaveHappenedOnceExactly();
        A.CallTo(() => fixtureState.Reset(3)).MustHaveHappenedOnceExactly();
        Assert.Single(session.PublishedMessages);
        Assert.IsType<Messaging.Contracts.Events.v1.GameweekJustBegan>(session.PublishedMessages[0].Message);
    }

    [Fact]
    public async Task OnGameweekTransition_WithFollowingOngoing_RefreshesState()
    {
        var gameweekClient = A.Fake<IGlobalSettingsClient>();
        A.CallTo(() => gameweekClient.GetGlobalSettings())
            .Returns(GameweeksBeforeTransition()).Once()
            .Then.Returns(GameweeksAfterTransition());

        var (action, fixtureState, lineupState, session) = BuildMonitor(gameweekClient);

        await action.EveryOtherMinuteTick(CancellationToken.None);
        await action.EveryOtherMinuteTick(CancellationToken.None);
        await action.EveryOtherMinuteTick(CancellationToken.None);

        A.CallTo(() => fixtureState.Reset(2)).MustHaveHappenedOnceExactly();
        A.CallTo(() => fixtureState.Reset(3)).MustHaveHappenedOnceExactly();
        A.CallTo(() => fixtureState.Refresh(3)).MustHaveHappenedOnceExactly();
        Assert.Single(session.PublishedMessages);
        Assert.IsType<Messaging.Contracts.Events.v1.GameweekJustBegan>(session.PublishedMessages[0].Message);
    }

    [Fact]
    public async Task OnGameweekFinished_PublishesMassTransitEventAndRefreshesState()
    {
        var gameweekClient = A.Fake<IGlobalSettingsClient>();
        A.CallTo(() => gameweekClient.GetGlobalSettings())
            .Returns(GameweeksBeforeTransition()).Once()
            .Then.Returns(GameweeksWithCurrentNowMarkedAsFinished());

        var (action, fixtureState, lineupState, session) = BuildMonitor(gameweekClient);

        await action.EveryOtherMinuteTick(CancellationToken.None);
        await action.EveryOtherMinuteTick(CancellationToken.None);
        await action.EveryOtherMinuteTick(CancellationToken.None);

        A.CallTo(() => fixtureState.Reset(2)).MustHaveHappenedOnceExactly();
        A.CallTo(() => fixtureState.Refresh(2)).MustHaveHappenedOnceExactly();
        Assert.Single(session.PublishedMessages);
        Assert.IsType<Messaging.Contracts.Events.v1.GameweekFinished>(session.PublishedMessages[0].Message);
    }

    [Fact]
    public async Task OnNoChanges_NoMassTransitEventsPublished()
    {
        var gameweekClient = A.Fake<IGlobalSettingsClient>();
        A.CallTo(() => gameweekClient.GetGlobalSettings()).Returns(GameweeksWithCurrentNowMarkedAsFinished());

        var (action, fixtureState, lineupState, session) = BuildMonitor(gameweekClient);

        await action.EveryOtherMinuteTick(CancellationToken.None);
        await action.EveryOtherMinuteTick(CancellationToken.None);
        await action.EveryOtherMinuteTick(CancellationToken.None);

        A.CallTo(() => fixtureState.Reset(A<int>._)).MustHaveHappenedOnceExactly();
        Assert.Empty(session.PublishedMessages);
    }

    [Fact]
    public async Task InPreseason_InitializesAndRefreshesLineupState()
    {
        var gameweekClient = A.Fake<IGlobalSettingsClient>();
        A.CallTo(() => gameweekClient.GetGlobalSettings()).Returns(GlobalSettingsWithGameweeks(Preseason()));

        var (action, fixtureState, lineupState, session) = BuildMonitor(gameweekClient);

        await action.EveryOtherMinuteTick(CancellationToken.None);
        await action.EveryOtherMinuteTick(CancellationToken.None);

        A.CallTo(() => lineupState.Reset(A<int>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => lineupState.Refresh(1)).MustHaveHappenedOnceExactly();
        Assert.Empty(session.PublishedMessages);
    }

    [Fact]
    public async Task FromPreseason_ToGw1_PublishesGw1Start()
    {
        var gameweekClient = A.Fake<IGlobalSettingsClient>();
        A.CallTo(() => gameweekClient.GetGlobalSettings())
            .Returns(GlobalSettingsWithGameweeks(Preseason())).Once()
            .Then.Returns(GlobalSettingsWithGameweeks(Gw1Current()));

        var (action, fixtureState, lineupState, session) = BuildMonitor(gameweekClient);

        await action.EveryOtherMinuteTick(CancellationToken.None);
        await action.EveryOtherMinuteTick(CancellationToken.None);

        // Reset(1) fires twice: once on init (gw1 was IsNext), once when gw1 becomes IsCurrent
        A.CallTo(() => fixtureState.Reset(1)).MustHaveHappenedTwiceExactly();
        Assert.Single(session.PublishedMessages);
        Assert.IsType<Messaging.Contracts.Events.v1.GameweekJustBegan>(session.PublishedMessages[0].Message);
    }

    private static (GameweekLifecycleMonitor monitor, IFixtureState fixtureState, ILineupState lineupState, TestPublishEndpoint session) BuildMonitor(IGlobalSettingsClient gameweekClient)
    {
        var fixtureState = A.Fake<IFixtureState>();
        var lineupState = A.Fake<ILineupState>();
        var session = new TestPublishEndpoint();
        var monitor = new GameweekLifecycleMonitor(
            gameweekClient,
            A.Fake<ILogger<GameweekLifecycleMonitor>>(),
            new TestScopeFactory(session),
            fixtureState,
            lineupState);
        return (monitor, fixtureState, lineupState, session);
    }

    private List<Gameweek> Preseason() => new()
    {
        new() { Id = 1, IsCurrent = false, IsNext = true },
        new() { Id = 2 }
    };

    private List<Gameweek> Gw1Current() => new()
    {
        TestBuilder.CurrentGameweek(1),
        TestBuilder.NextGameweek(2)
    };

    private static List<Gameweek> SomeGameweeks() => new()
    {
        TestBuilder.PreviousGameweek(1),
        TestBuilder.CurrentGameweek(2),
        TestBuilder.NextGameweek(3)
    };

    private static GlobalSettings GameweeksBeforeTransition() => GlobalSettingsWithGameweeks(SomeGameweeks());

    private static GlobalSettings GameweeksAfterTransition() => GlobalSettingsWithGameweeks(new List<Gameweek>
    {
        TestBuilder.OlderGameweek(1),
        TestBuilder.PreviousGameweek(2),
        TestBuilder.CurrentGameweek(3)
    });

    private static GlobalSettings GameweeksWithCurrentNowMarkedAsFinished()
    {
        var currentGameweek = TestBuilder.CurrentGameweek(2);
        currentGameweek.IsFinished = true;
        return GlobalSettingsWithGameweeks(new List<Gameweek>
        {
            TestBuilder.PreviousGameweek(1),
            currentGameweek,
            TestBuilder.NextGameweek(3)
        });
    }

    private static GlobalSettings GlobalSettingsWithGameweeks(List<Gameweek> gameweeks) =>
        new() { Gameweeks = gameweeks };
}
