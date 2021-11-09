using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.Workers.Events;
using Fpl.Workers.RecurringActions;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;

namespace FplBot.Tests;

public class GameweekLifecycleRecurringActionTests
{
    [Fact]
    public async Task OnFirstProcess_NoCurrentGameweek_OrchestratesNothing()
    {
        var gameweekClient = A.Fake<IGlobalSettingsClient>();

        A.CallTo(() => gameweekClient.GetGlobalSettings()).Returns(GlobalSettingsWithGameweeks(new List<Gameweek>()));

        var mediator = A.Fake<IMediator>();
        var session = new TestableMessageSession();
        var action = new GameweekLifecycleMonitor(gameweekClient, A.Fake<ILogger<GameweekLifecycleMonitor>>(), mediator, session);

        await action.EveryOtherMinuteTick(CancellationToken.None);

        A.CallTo(() => mediator.Publish(null, CancellationToken.None)).WithAnyArguments().MustNotHaveHappened();
        Assert.Empty(session.PublishedMessages);
    }

    [Fact]
    public async Task OnFirstProcess_OrchestratesInitializeAndGameweekOngoing()
    {
        var gameweekClient = A.Fake<IGlobalSettingsClient>();

        A.CallTo(() => gameweekClient.GetGlobalSettings()).Returns(GlobalSettingsWithGameweeks(SomeGameweeks()));

        var mediator = A.Fake<IMediator>();
        var session = new TestableMessageSession();
        var action = new GameweekLifecycleMonitor(gameweekClient, A.Fake<ILogger<GameweekLifecycleMonitor>>(), mediator, session);

        await action.EveryOtherMinuteTick(CancellationToken.None);

        A.CallTo(() => mediator.Publish(A<GameweekMonitoringStarted>.That.Matches(a => a.CurrentGameweek.Id == 2), CancellationToken.None)).MustHaveHappenedOnceExactly();
        A.CallTo(() => mediator.Publish(A<GameweekCurrentlyOnGoing>.That.Matches(a => a.Gameweek.Id == 2), CancellationToken.None)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task OnGameweekTransition_CallsOrchestratorBegin()
    {
        var gameweekClient = A.Fake<IGlobalSettingsClient>();
        A.CallTo(() => gameweekClient.GetGlobalSettings())
            .Returns(GameweeksBeforeTransition()).Once()
            .Then.Returns(GameweeksAfterTransition());

        var mediator = A.Fake<IMediator>();
        var session = new TestableMessageSession();
        var action = new GameweekLifecycleMonitor(gameweekClient, A.Fake<ILogger<GameweekLifecycleMonitor>>(), mediator, session);

        await action.EveryOtherMinuteTick(CancellationToken.None);

        A.CallTo(() => mediator.Publish(A<GameweekMonitoringStarted>.That.Matches(a => a.CurrentGameweek.Id == 2), CancellationToken.None)).MustHaveHappenedOnceExactly();
        A.CallTo(() => mediator.Publish(A<GameweekCurrentlyOnGoing>.That.Matches(a => a.Gameweek.Id == 2), CancellationToken.None)).MustHaveHappenedOnceExactly();

        await action.EveryOtherMinuteTick(CancellationToken.None);

        A.CallTo(() => mediator.Publish(A<GameweekJustBegan>.That.Matches(a => a.Gameweek.Id == 3), CancellationToken.None)).MustHaveHappenedOnceExactly();
        Assert.Single(session.PublishedMessages);
        Assert.IsType<Messaging.Contracts.Events.v1.GameweekJustBegan>(session.PublishedMessages[0].Message);
    }

    [Fact]
    public async Task OnGameweekFinished_CallsOrchestratorEnd()
    {
        var gameweekClient = A.Fake<IGlobalSettingsClient>();
        A.CallTo(() => gameweekClient.GetGlobalSettings())
            .Returns(GameweeksBeforeTransition()).Once()
            .Then.Returns(GameweeksWithCurrentNowMarkedAsFinished());

        var mediator = A.Fake<IMediator>();
        var session = new TestableMessageSession();
        var action = new GameweekLifecycleMonitor(gameweekClient, A.Fake<ILogger<GameweekLifecycleMonitor>>(), mediator, session);

        await action.EveryOtherMinuteTick(CancellationToken.None);

        A.CallTo(() => mediator.Publish(A<GameweekMonitoringStarted>.That.Matches(a => a.CurrentGameweek.Id == 2), CancellationToken.None)).MustHaveHappenedOnceExactly();
        A.CallTo(() => mediator.Publish(A<GameweekCurrentlyOnGoing>.That.Matches(a => a.Gameweek.Id == 2), CancellationToken.None)).MustHaveHappenedOnceExactly();

        await action.EveryOtherMinuteTick(CancellationToken.None);

        A.CallTo(() => mediator.Publish(A<GameweekFinished>.That.Matches(a => a.Gameweek.Id == 2), CancellationToken.None)).MustHaveHappenedOnceExactly();          }

    [Fact]
    public async Task OnNoChanges_CallsNothing()
    {
        var gameweekClient = A.Fake<IGlobalSettingsClient>();

        A.CallTo(() => gameweekClient.GetGlobalSettings()).Returns(GameweeksWithCurrentNowMarkedAsFinished());

        var mediator = A.Fake<IMediator>();
        var session = new TestableMessageSession();
        var action = new GameweekLifecycleMonitor(gameweekClient, A.Fake<ILogger<GameweekLifecycleMonitor>>(), mediator, session);

        await action.EveryOtherMinuteTick(CancellationToken.None);


        A.CallTo(() => mediator.Publish(A<GameweekMonitoringStarted>.That.Matches(a => a.CurrentGameweek.Id == 2), CancellationToken.None)).MustHaveHappenedOnceExactly();

        A.CallTo(() => mediator.Publish(A<GameweekJustBegan>._, CancellationToken.None)).WithAnyArguments().MustNotHaveHappened();
        A.CallTo(() => mediator.Publish(A<GameweekCurrentlyOnGoing>._, CancellationToken.None)).WithAnyArguments().MustNotHaveHappened();
        A.CallTo(() => mediator.Publish(A<GameweekFinished>._, CancellationToken.None)).WithAnyArguments().MustNotHaveHappened();


        await action.EveryOtherMinuteTick(CancellationToken.None);

        A.CallTo(() => mediator.Publish(A<GameweekJustBegan>._, CancellationToken.None)).WithAnyArguments().MustNotHaveHappened();
        A.CallTo(() => mediator.Publish(A<GameweekCurrentlyOnGoing>._, CancellationToken.None)).WithAnyArguments().MustNotHaveHappened();
        A.CallTo(() => mediator.Publish(A<GameweekFinished>._, CancellationToken.None)).WithAnyArguments().MustNotHaveHappened();


        await action.EveryOtherMinuteTick(CancellationToken.None);

        A.CallTo(() => mediator.Publish(A<GameweekJustBegan>._, CancellationToken.None)).WithAnyArguments().MustNotHaveHappened();
        A.CallTo(() => mediator.Publish(A<GameweekCurrentlyOnGoing>._, CancellationToken.None)).WithAnyArguments().MustNotHaveHappened();
        A.CallTo(() => mediator.Publish(A<GameweekFinished>._, CancellationToken.None)).WithAnyArguments().MustNotHaveHappened();
    }

    private static List<Gameweek> SomeGameweeks()
    {
        return new List<Gameweek>
        {
            TestBuilder.PreviousGameweek(1),
            TestBuilder.CurrentGameweek(2),
            TestBuilder.NextGameweek(3)
        };
    }

    private static GlobalSettings GameweeksBeforeTransition()
    {
        return GlobalSettingsWithGameweeks(SomeGameweeks());
    }

    private static GlobalSettings GameweeksAfterTransition()
    {
        return GlobalSettingsWithGameweeks(new List<Gameweek>
        {
            TestBuilder.OlderGameweek(1),
            TestBuilder.PreviousGameweek(2),
            TestBuilder.CurrentGameweek(3)
        });
    }

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

    private static GlobalSettings GlobalSettingsWithGameweeks(List<Gameweek> gameweeks)
    {
        return new GlobalSettings {Gameweeks = gameweeks};
    }
}