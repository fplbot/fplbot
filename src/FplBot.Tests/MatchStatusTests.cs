using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Core.Abstractions;
using FplBot.Core.GameweekLifecycle;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FplBot.Tests
{
    public class MatchStatusTests
    {
        [Fact]
        public async Task DoesNotEmitInInitPhase()
        {
            var monitor = CreateNewLineupScenario();
            var wasCalled = false;
            monitor.OnLineUpReady += details =>
            {
                wasCalled = true;
                return Task.CompletedTask;
            };
            await monitor.Reset(1);
            Assert.False(wasCalled);
        }

        [Fact]
        public async Task WhenLineupsInAFixture_EmitsEvent()
        {
            var monitor = CreateNewLineupScenario();
            var wasCalled = false;
            monitor.OnLineUpReady += details =>
            {
                wasCalled = true;
                return Task.CompletedTask;
            };
            await monitor.Reset(1);
            await monitor.Refresh(1);
            
            Assert.True(wasCalled);
        }
        
        [Fact]
        public async Task WhenLineupsInSingleFixture_SequencialRefreshes_EmitsEventOnlyOnce()
        {
            var monitor = CreateNewLineupScenario();
            var wasCalledCount = 0;
            monitor.OnLineUpReady += details =>
            {
                wasCalledCount++;
                return Task.CompletedTask;
            };
            
            await monitor.Reset(1);
            
            await monitor.Refresh(1);
            await monitor.Refresh(1);
            
            Assert.Equal(1, wasCalledCount);
        }
        
        [Fact]
        public async Task WhenLineupsInTwoFixtures_SequencialRefreshes_EmitsOneEventPrFixture()
        {
            var monitor = CreateTwoNewLineupsScenario();
            var callCountFixture1 = 0;
            var callCountFixture2 = 0;
            monitor.OnLineUpReady += details =>
            {
                if (details.FixturePulseId == TestBuilder.NoGoals(1).PulseId)
                    callCountFixture1++;
                
                if (details.FixturePulseId == TestBuilder.NoGoals(2).PulseId)
                    callCountFixture2++;
                return Task.CompletedTask;
            };
            
            await monitor.Reset(1);
            
            await monitor.Refresh(1);
            
            
            Assert.Equal(1, callCountFixture1);
            Assert.Equal(1, callCountFixture2);
        }

        private static MatchState CreateNewLineupScenario()
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
            
            return new MatchState(fixtureClient, scraperFake, A.Fake<ILogger<MatchState>>());
        }
        
        private static MatchState CreateTwoNewLineupsScenario()
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
            
            return new MatchState(fixtureClient, scraperFake, A.Fake<ILogger<MatchState>>());
        }
    }
}