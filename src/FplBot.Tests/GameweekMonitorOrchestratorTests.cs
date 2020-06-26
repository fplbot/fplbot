using System.Threading.Tasks;
using FakeItEasy;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.GameweekLifecycle;
using Slackbot.Net.Extensions.FplBot.RecurringActions;
using Xunit;

namespace FplBot.Tests
{
    public class GameweekMonitorOrchestratorTests
    {
        [Fact]
        public void WhenGameweek_HasJustBegun_TriggersEvent()
        {
            var orchestrator = CreateOrchestrator();
            
            int? gwId = null;
            orchestrator.GameWeekJustBeganEventHandlers +=  i =>
            {
                gwId = i;
                return Task.CompletedTask;
            };

            orchestrator.GameweekJustBegan(1);
            
            Assert.NotNull(gwId);
            Assert.Equal(1, gwId.Value);
        }

        [Fact]
        public void WhenGameweekIsOngoing_HasJustBegun_TriggersEvent()
        {
            var orchestrator = CreateOrchestrator();
            
            int? gwId = null;
            orchestrator.GameweekIsCurrentlyOngoingEventHandlers +=  i =>
            {
                gwId = i;
                return Task.CompletedTask;
            };

            orchestrator.GameweekIsCurrentlyOngoing(1);
            
            Assert.NotNull(gwId);
            Assert.Equal(1, gwId.Value);
        }

        private static GameweekMonitorOrchestrator CreateOrchestrator()
        {
            var orchestrator = new GameweekMonitorOrchestrator(A.Fake<IHandleGameweekStarted>(), A.Fake<IMonitorFixtureEvents>());
            return orchestrator;
        }
    }
}