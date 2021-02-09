using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Core.Abstractions;
using FplBot.Core.GameweekLifecycle;
using FplBot.Core.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FplBot.Tests
{
    public class MatchStatusTests
    {
        private IMediator _mediator;

        [Fact]
        public async Task DoesNotEmitInInitPhase()
        {
            var monitor = CreateNewLineupScenario();            
            await monitor.Reset(1);
            
            A.CallTo(() => _mediator.Publish(A<LineupReady>._, A<CancellationToken>._)).MustNotHaveHappened();
        }

        [Fact]
        public async Task WhenLineupsInAFixture_EmitsEvent()
        {
            var monitor = CreateNewLineupScenario();    
            await monitor.Reset(1);
            await monitor.Refresh(1);
            
            A.CallTo(() => _mediator.Publish(A<LineupReady>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }
        
        [Fact]
        public async Task WhenLineupsInSingleFixture_SequencialRefreshes_EmitsEventOnlyOnce()
        {
            var monitor = CreateNewLineupScenario(); 
            
            await monitor.Reset(1);
            
            await monitor.Refresh(1);
            await monitor.Refresh(1);
            
            A.CallTo(() => _mediator.Publish(A<LineupReady>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        }
        
        [Fact]
        public async Task WhenLineupsInTwoFixtures_SequencialRefreshes_EmitsOneEventPrFixture()
        {
            var monitor = CreateTwoNewLineupsScenario();
            await monitor.Reset(1);
            await monitor.Refresh(1);

            A.CallTo(() => _mediator.Publish(A<LineupReady>._, A<CancellationToken>._)).MustHaveHappenedTwiceExactly();
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
            _mediator = A.Fake<IMediator>();
            return new LineupState(fixtureClient, scraperFake, _mediator, A.Fake<ILogger<LineupState>>());
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
            _mediator = A.Fake<IMediator>();
            return new LineupState(fixtureClient, scraperFake, _mediator, A.Fake<ILogger<LineupState>>());
        }
    }
}