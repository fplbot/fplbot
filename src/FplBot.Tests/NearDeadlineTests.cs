using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Core.Helpers;
using FplBot.Core.Models;
using FplBot.Core.RecurringActions;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Tests.Helpers;
using MediatR;
using Microsoft.Extensions.Logging;
using NServiceBus.Testing;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class NearDeadlineTests
    {
        private readonly ITestOutputHelper _helper;
        private DateTimeUtils _deadlineChecker;

        public NearDeadlineTests(ITestOutputHelper helper)
        {
            _helper = helper;
            _deadlineChecker = Factory.Create<DateTimeUtils>();
        }

        [Fact]
        public void WhenDayBefore()
        {
            _deadlineChecker.NowUtcOverride = new DateTime(2005, 5, 24, 19, 0, 0);
            var deadline = new DateTime(2005, 5, 25, 20, 0, 0);
            Assert.False(_deadlineChecker.IsWithinMinutesToDate(60, deadline));
        }

        [Fact]
        public void WhenBeforeTheMinute()
        {
            _deadlineChecker.NowUtcOverride = new DateTime(2005, 5, 25, 19, 59, 59);
            var deadline = new DateTime(2005, 5, 25, 20, 0, 0);
            Assert.False(_deadlineChecker.IsWithinMinutesToDate(60, deadline));
        }

        [Fact]
        public void WhenIsAnySecondWithTheMinute()
        {
            var deadline = new DateTime(2005, 5, 25, 20, 0, 0);

            for(var i = 0; i < 60; i++)
            {
                _deadlineChecker.NowUtcOverride = new DateTime(2005, 5, 25, 19, 0, i);
                var isTheMinute = _deadlineChecker.IsWithinMinutesToDate(60, deadline);
                if (!isTheMinute)
                {
                    _helper.WriteLine($"Not true for {i} - {_deadlineChecker.NowUtcOverride-deadline}");
                }

                Assert.True(isTheMinute);
            }
        }

        [Fact]
        public void WhenIs()
        {
            var deadline = new DateTime(2020, 7, 4, 10, 30, 0);
            _deadlineChecker.NowUtcOverride = new DateTime(2020, 7, 4, 10, 0, 20);
            var isTheMinute = _deadlineChecker.IsWithinMinutesToDate(30, deadline);
            Assert.True(isTheMinute);
        }

        [Fact]
        public void WhenPassedTheMinute()
        {
            _deadlineChecker.NowUtcOverride = new DateTime(2005, 5, 25, 19, 1, 0);
            var deadline = new DateTime(2005, 5, 25, 20, 0, 0);
            Assert.False(_deadlineChecker.IsWithinMinutesToDate(60, deadline));
        }

        [Fact]
        public void WhenAnotherHourTheSameDayButSameMinute()
        {
            _deadlineChecker.NowUtcOverride = new DateTime(2005, 5, 25, 20, 0, 0);
            var deadline = new DateTime(2005, 5, 25, 20, 0, 0);
            Assert.False(_deadlineChecker.IsWithinMinutesToDate(60, deadline));
        }

        [Fact]
        public void WhenTheDayAfterButMinute()
        {
            _deadlineChecker.NowUtcOverride = new DateTime(2005, 5, 26, 19, 0, 0);
            var deadline = new DateTime(2005, 5, 25, 20, 0, 0);
            Assert.False(_deadlineChecker.IsWithinMinutesToDate(60, deadline));
        }

        [Fact]
        public void NoOverride()
        {
            _deadlineChecker.NowUtcOverride = new DateTime(2005, 5, 26, 19, 0, 0);
            var deadline = new DateTime(2005, 5, 25, 20, 0, 0);
            Assert.False(_deadlineChecker.IsWithinMinutesToDate(60, deadline));
        }

        [Fact]
        public void When24hBefore()
        {
            _deadlineChecker.NowUtcOverride = new DateTime(2005, 5, 24, 20, 0, 30);
            var deadline = new DateTime(2005, 5, 25, 20, 0, 0);
            Assert.True(_deadlineChecker.IsWithinMinutesToDate(60*24, deadline));
        }

        [Fact]
        public void When23hBefore()
        {
            _deadlineChecker.NowUtcOverride = new DateTime(2005, 5, 24, 21, 0, 30);
            var deadline = new DateTime(2005, 5, 25, 20, 0, 0);
            Assert.False(_deadlineChecker.IsWithinMinutesToDate(60*24, deadline));
        }

        [Fact]
        public async Task OnlyPublishesOnceForFirstGameweek()
        {
            var fakeSettingsClient = A.Fake<IGlobalSettingsClient>();
            var gameweek1 = new Gameweek { IsCurrent = false, IsNext = true, Deadline = new DateTime(2021,8,15,10,0,0)};
            var gameweek2 = new Gameweek { IsCurrent = false, IsNext = false, Deadline = new DateTime(2021,8,22,10,0,0)};
            var globalSettings = new GlobalSettings { Gameweeks = new List<Gameweek> { gameweek1, gameweek2 } };
            A.CallTo(() => fakeSettingsClient.GetGlobalSettings()).Returns(globalSettings);
            var session = new TestableMessageSession();
            var dontCareLogger = A.Fake<ILogger<NearDeadLineMonitor>>();
            var dateTimeUtils = new DateTimeUtils { NowUtcOverride = new DateTime(2021, 8, 14, 10, 0, 0) };

            var handler = new NearDeadLineMonitor(fakeSettingsClient, dateTimeUtils, session, dontCareLogger);

            await handler.EveryMinuteTick();

            Assert.Equal(1, session.PublishedMessages.Length);
            Assert.IsType<TwentyFourHoursToDeadline>(session.PublishedMessages[0].Message);
        }

        [Fact]
        public async Task OnlyPublishesOnceForSecondGameweekWhenFirstGameweekIsCurrent()
        {
            var fakeSettingsClient = A.Fake<IGlobalSettingsClient>();
            var gameweek1 = new Gameweek { IsCurrent = true, IsNext = false, Deadline = new DateTime(2021,8,15,10,0,0)};
            var gameweek2 = new Gameweek { IsCurrent = false, IsNext = true, Deadline = new DateTime(2021,8,22,10,0,0)};
            var globalSettings = new GlobalSettings { Gameweeks = new List<Gameweek> { gameweek1, gameweek2 } };
            A.CallTo(() => fakeSettingsClient.GetGlobalSettings()).Returns(globalSettings);
            var session = new TestableMessageSession();
            var dontCareLogger = A.Fake<ILogger<NearDeadLineMonitor>>();
            var dateTimeUtils = new DateTimeUtils { NowUtcOverride = new DateTime(2021, 8, 21, 10, 0, 0) };

            var handler = new NearDeadLineMonitor(fakeSettingsClient, dateTimeUtils, session, dontCareLogger);

            await handler.EveryMinuteTick();

            Assert.Equal(1, session.PublishedMessages.Length);
            Assert.IsType<TwentyFourHoursToDeadline>(session.PublishedMessages[0].Message);
        }
    }
}
