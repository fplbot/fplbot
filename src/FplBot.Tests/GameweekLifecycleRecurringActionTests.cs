using System.Collections.Generic;
using System.Threading.Tasks;
using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.RecurringActions;
using Xunit;

namespace FplBot.Tests
{
    public class GameweekLifecycleRecurringActionTests
    {
        [Fact]
        public async Task OnFirstProcess_NoCurrentGameweek_OrchestratesNothing()
        {
            var gameweekClient = A.Fake<IGameweekClient>();
           
            A.CallTo(() => gameweekClient.GetGameweeks()).Returns(new List<Gameweek>());
            
            var orchestrator = A.Fake<IGameweekMonitorOrchestrator>();
            var action = new GameweekLifecycleRecurringAction(gameweekClient, A.Fake<ILogger<GameweekLifecycleRecurringAction>>(), orchestrator);
            
            await action.Process();
            
            A.CallTo(() => orchestrator.Initialize(A<int>._)).MustNotHaveHappened();
            A.CallTo(() => orchestrator.GameweekJustBegan(A<int>._)).MustNotHaveHappened();
            A.CallTo(() => orchestrator.GameweekIsCurrentlyOngoing(A<int>._)).MustNotHaveHappened();
        }
        
        [Fact]
        public async Task OnFirstProcess_OrchestratesInitializeAndGameweekOngoing()
        {
            var gameweekClient = A.Fake<IGameweekClient>();
           
            A.CallTo(() => gameweekClient.GetGameweeks()).Returns(SomeGameweeks());
            
            var orchestrator = A.Fake<IGameweekMonitorOrchestrator>();
            var action = new GameweekLifecycleRecurringAction(gameweekClient, A.Fake<ILogger<GameweekLifecycleRecurringAction>>(), orchestrator);
            
            await action.Process();
            
            A.CallTo(() => orchestrator.Initialize(2)).MustHaveHappenedOnceExactly();
            A.CallTo(() => orchestrator.GameweekIsCurrentlyOngoing(2)).MustHaveHappenedOnceExactly();
        }
        
        [Fact]
        public async Task OnGameweekTransition_CallsOrchestratorBegin()
        {
            var gameweekClient = A.Fake<IGameweekClient>();
            A.CallTo(() => gameweekClient.GetGameweeks())
                .Returns(GameweeksBeforeTransition()).Once()
                .Then.Returns(GameweeksAfterTransition());
            
            var orchestrator = A.Fake<IGameweekMonitorOrchestrator>();
            var action = new GameweekLifecycleRecurringAction(gameweekClient, A.Fake<ILogger<GameweekLifecycleRecurringAction>>(), orchestrator);
            
            await action.Process();
            await action.Process();
            
            A.CallTo(() => orchestrator.Initialize(2)).MustHaveHappenedOnceExactly();
            A.CallTo(() => orchestrator.GameweekIsCurrentlyOngoing(2)).MustHaveHappenedOnceExactly();
            A.CallTo(() => orchestrator.GameweekJustBegan(3)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task OnGameweekFinished_CallsOrchestratorEnd()
        {
            var gameweekClient = A.Fake<IGameweekClient>();
            A.CallTo(() => gameweekClient.GetGameweeks())
                .Returns(GameweeksBeforeTransition()).Once()
                .Then.Returns(GameweeksWithCurrentNowMarkedAsFinished());
            
            var orchestrator = A.Fake<IGameweekMonitorOrchestrator>();
            var action = new GameweekLifecycleRecurringAction(gameweekClient, A.Fake<ILogger<GameweekLifecycleRecurringAction>>(), orchestrator);
            
            await action.Process();
            
            A.CallTo(() => orchestrator.Initialize(2)).MustHaveHappenedOnceExactly();
            A.CallTo(() => orchestrator.GameweekIsCurrentlyOngoing(2)).MustHaveHappenedOnceExactly();
            
            await action.Process();

            A.CallTo(() => orchestrator.GameweekJustEnded(2)).MustHaveHappenedOnceExactly();
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

        private static List<Gameweek> GameweeksBeforeTransition()
        {
            return SomeGameweeks();
        }

        private static List<Gameweek> GameweeksAfterTransition()
        {
            return new List<Gameweek>
            {
                TestBuilder.OlderGameweek(1),
                TestBuilder.PreviousGameweek(2),
                TestBuilder.CurrentGameweek(3)
            };
        }
        
        private static List<Gameweek> GameweeksWithCurrentNowMarkedAsFinished()
        {
            var currentGameweek = TestBuilder.CurrentGameweek(2);
            currentGameweek.IsFinished = true;
            return new List<Gameweek>
            {
                TestBuilder.PreviousGameweek(1),
                currentGameweek,
                TestBuilder.NextGameweek(3)
            };
        }
    }
}