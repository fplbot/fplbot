using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using Slackbot.Net.Endpoints.Models;
using Slackbot.Net.Extensions.FplBot;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Xunit;

namespace FplBot.Tests
{
    public class StateTests
    {
        [Fact]
        public async Task DoesNotCrashWithNoDataReturned()
        {
            var state = CreateAllMockState();
            await state.Reset(1);
            var current = state.GetLeagues();
            Assert.Empty(current);
            
            var contextForLeague = state.GetGameweekLeagueContext(1337);
            Assert.Empty(contextForLeague.Players);
            Assert.Empty(contextForLeague.Teams);
            Assert.Empty(contextForLeague.TransfersForLeague);
        }

        [Fact]
        public async Task WithGoalScoredEvent()
        {
            var state = CreateGoalScoredScenario();
            await state.Reset(1);
            var newEvents = await state.Refresh(1);
            Assert.NotEmpty(newEvents);
            
            var goalEvent = newEvents.First().StatMap[StatType.GoalsScored].First();
            Assert.Equal(PlayerEvent.TeamType.Away, goalEvent.Team);
            Assert.Equal(1337, goalEvent.PlayerId);
        }

        private static IState CreateAllMockState()
        {
            return new State(A.Fake<IFixtureClient>(),
                A.Fake<IPlayerClient>(),
                A.Fake<ITeamsClient>(),
                A.Fake<ISlackTeamRepository>(),
                A.Fake<ITransfersByGameWeek>());
        }
        
        private static IState CreateGoalScoredScenario()
        {
            var playerClient = A.Fake<IPlayerClient>();
            A.CallTo(() => playerClient.GetAllPlayers()).Returns(new List<Player>
            {
                new Player
                {
                    Id = 1
                }
            });
            
            var slackTeamRepository = A.Fake<ISlackTeamRepository>();
            A.CallTo(() => slackTeamRepository.GetAllTeamsAsync()).Returns(new List<SlackTeam>{ new SlackTeam
            {
                FplbotLeagueId = 111,
            }});
            
            var transfersByGameWeek = A.Fake<ITransfersByGameWeek>();
            A.CallTo(() => transfersByGameWeek.GetTransfersByGameweek(1, 111)).Returns(new List<TransfersByGameWeek.Transfer>{ new TransfersByGameWeek.Transfer { EntryId = 2 }});

            var teamsClient = A.Fake<ITeamsClient>();
            A.CallTo(() => teamsClient.GetAllTeams()).Returns(new List<Team>
            {
               TestBuilder.AwayTeam()
            });


            var fixtureClient = A.Fake<IFixtureClient>();
            A.CallTo(() => fixtureClient.GetFixturesByGameweek(1)).Returns(new List<Fixture>
            {
                TestBuilder.AwayTeamGoal(888, 1)
            }).Once();
            
            A.CallTo(() => fixtureClient.GetFixturesByGameweek(1)).Returns(new List<Fixture>
            {
                TestBuilder.AwayTeamGoal(888, 2)
            }).Once();
            
            return new State(fixtureClient,
                playerClient,
                teamsClient,
                slackTeamRepository,
                transfersByGameWeek);
        }
    }
}