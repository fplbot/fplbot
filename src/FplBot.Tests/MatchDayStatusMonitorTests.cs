using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.EventPublishers.RecurringActions;
using Fpl.EventPublishers.States;
using FplBot.Messaging.Contracts.Events.v1;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;

namespace FplBot.Tests;

public class MatchDayStatusMonitorTests
{
    [Fact]
    public void AppStartupDoesNotEmitEvents()
    {
        var res = Some.EventStatusResponse() with { Status = Some.EventStatusListWithoutAnythingAdded() };
        A.CallTo(() => EventStatusClient.GetEventStatus()).Returns(res);

        var monitor = CreateMatchDayStatusMonitor();
        monitor.EveryFiveMinutesTick(CancellationToken.None);

        Assert.Empty(Mediator.PublishedMessages);
    }

    [Fact]
    public void NextCheck_NoChanges_DoesNotEmitEvent()
    {
        var res = Some.EventStatusResponse() with { Status = Some.EventStatusListWithoutAnythingAdded() };
        A.CallTo(() => EventStatusClient.GetEventStatus()).Returns(res);

        var monitor = CreateMatchDayStatusMonitor();

        monitor.EveryFiveMinutesTick(CancellationToken.None);
        monitor.EveryFiveMinutesTick(CancellationToken.None);

        Assert.Empty(Mediator.PublishedMessages);
    }

    [Fact]
    public void NextCheck_BonusTrue_EmitsBonusAdded()
    {
        var res = Some.EventStatusResponse() with { Status = Some.EventStatusListWithoutAnythingAdded() };
        var res2 = Some.EventStatusResponse() with { Status = Some.EventStatusListBonusTrue() };

        A.CallTo(() => EventStatusClient.GetEventStatus()).Returns(value: res).Once().Then.Returns(res2);

        var monitor = CreateMatchDayStatusMonitor();

        monitor.EveryFiveMinutesTick(CancellationToken.None);
        monitor.EveryFiveMinutesTick(CancellationToken.None);

        Assert.Single(Mediator.PublishedMessages);
        Assert.IsType<MatchdayBonusPointsAdded>(Mediator.PublishedMessages[0].Message);
    }

    [Fact]
    public void NextCheck_BonusTrueToBonusTrue_EmitsOnlyOnce()
    {
        var res = Some.EventStatusResponse() with { Status = Some.EventStatusListWithoutAnythingAdded() };
        var res2 = Some.EventStatusResponse() with { Status = Some.EventStatusListBonusTrue() };


        A.CallTo(() => EventStatusClient.GetEventStatus()).Returns(value: res).Once().Then.Returns(res2);

        var monitor = CreateMatchDayStatusMonitor();

        monitor.EveryFiveMinutesTick(CancellationToken.None);
        monitor.EveryFiveMinutesTick(CancellationToken.None);
        monitor.EveryFiveMinutesTick(CancellationToken.None);

        Assert.Single(Mediator.PublishedMessages);
        Assert.IsType<MatchdayBonusPointsAdded>(Mediator.PublishedMessages[0].Message);
    }

    [Fact]
    public void NextCheck_PointsReady_EmitsPointsReady()
    {
        var first = Some.EventStatusResponse() with { Status = Some.EventStatusListWithoutAnythingAdded() };
        var then = Some.EventStatusResponse() with { Status = Some.EventStatusListPointsReady() };


        A.CallTo(() => EventStatusClient.GetEventStatus()).Returns(first).Once().Then.Returns(then);

        var monitor = CreateMatchDayStatusMonitor();

        monitor.EveryFiveMinutesTick(CancellationToken.None);
        monitor.EveryFiveMinutesTick(CancellationToken.None);

        Assert.Single(Mediator.PublishedMessages);
        Assert.IsType<MatchdayMatchPointsAdded>(Mediator.PublishedMessages[0].Message);
    }

    [Fact]
    public void NextCheck_PointsReadyToPointsReady_OnlyEmitsOnce()
    {
        var first = Some.EventStatusResponse() with { Status = Some.EventStatusListWithoutAnythingAdded() };
        var then = Some.EventStatusResponse() with { Status = Some.EventStatusListPointsReady() };


        A.CallTo(() => EventStatusClient.GetEventStatus()).Returns(first).Once().Then.Returns(then);

        var monitor = CreateMatchDayStatusMonitor();

        monitor.EveryFiveMinutesTick(CancellationToken.None);
        monitor.EveryFiveMinutesTick(CancellationToken.None);
        monitor.EveryFiveMinutesTick(CancellationToken.None);

        Assert.Single(Mediator.PublishedMessages);
        Assert.IsType<MatchdayMatchPointsAdded>(Mediator.PublishedMessages[0].Message);
    }

    public MatchDayStatusMonitor CreateMatchDayStatusMonitor( )
    {
        return new(EventStatusClient, Mediator, A.Fake<ILogger<MatchDayStatusMonitor>>());
    }

    private IEventStatusClient EventStatusClient = A.Fake<IEventStatusClient>();
    private TestableMessageSession Mediator = new();

    private static class Some
    {
        public static EventStatusResponse EventStatusResponse()
        {
            return new();
        }

        public static ICollection<EventStatus> EventStatusListWithoutAnythingAdded()
        {
            return new List<EventStatus> { EventStatusWithoutAnythingAdded() };
        }

        public static ICollection<EventStatus> EventStatusListBonusTrue()
        {
            return new List<EventStatus> { EventStatusWithoutAnythingAdded() with
            {
                PointsStatus = EventStatusConstants.PointStatus.Nothing,
                BonusAdded = true
            } };
        }

        public static ICollection<EventStatus> EventStatusListPointsReady()
        {
            return new List<EventStatus> { EventStatusWithoutAnythingAdded() with { PointsStatus = EventStatusConstants.PointStatus.Ready} };
        }

        public static EventStatus EventStatusWithoutAnythingAdded()
        {
            return new()
            {
                Date = "2020-01-01",
                Event = 1,
                BonusAdded = false,
                PointsStatus = EventStatusConstants.PointStatus.Nothing
            };
        }
    }
}
