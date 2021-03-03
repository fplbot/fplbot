using System.Collections.Generic;
using System.Threading;
using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Fpl.Workers.RecurringActions;
using FplBot.Core.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FplBot.Tests
{
    public class MatchDayStatusMonitorTests
    {
        [Fact]
        public void AppStartupDoesNotEmitEvents()
        {
            var res = Some.EventStatusResponse() with { Status = Some.EventStatusListWithoutAnythingAdded() };
            A.CallTo(() => EventStatusClient.GetEventStatus()).Returns(res);

            var monitor = CreateMatchDayStatusMonitor();
            monitor.EveryFiveMinutesTick(CancellationToken.None);

            A.CallTo(() => Mediator.Publish(A<object>._, A<CancellationToken>._)).MustNotHaveHappened();
        }

        [Fact]
        public void NextCheck_NoChanges_DoesNotEmitEvent()
        {
            var res = Some.EventStatusResponse() with { Status = Some.EventStatusListWithoutAnythingAdded() };
            A.CallTo(() => EventStatusClient.GetEventStatus()).Returns(res);

            var monitor = CreateMatchDayStatusMonitor();

            monitor.EveryFiveMinutesTick(CancellationToken.None);
            monitor.EveryFiveMinutesTick(CancellationToken.None);

            A.CallTo(() => Mediator.Publish(A<object>._, A<CancellationToken>._)).MustNotHaveHappened();
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

            A.CallTo(() => Mediator.Publish(A<BonusAdded>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
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

            A.CallTo(() => Mediator.Publish(A<BonusAdded>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
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

            A.CallTo(() => Mediator.Publish(A<PointsReady>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
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

            A.CallTo(() => Mediator.Publish(A<PointsReady>._, CancellationToken.None)).MustHaveHappenedOnceExactly();
        }

        public MatchDayStatusMonitor CreateMatchDayStatusMonitor( )
        {
            return new(EventStatusClient, Mediator, A.Fake<ILogger<MatchDayStatusMonitor>>());
        }

        private IEventStatusClient EventStatusClient = A.Fake<IEventStatusClient>();
        private IMediator Mediator = A.Fake<IMediator>();

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
}
