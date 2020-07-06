using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Slackbot.Net.Dynamic;
using Slackbot.Net.Endpoints.Models;
using Slackbot.Net.Extensions.FplBot;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.GameweekLifecycle.Handlers;
using Slackbot.Net.Extensions.FplBot.Helpers;
using Slackbot.Net.SlackClients.Http;
using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class StateTests
    {
        private readonly ITestOutputHelper _helper;

        public StateTests(ITestOutputHelper helper)
        {
            _helper = helper;
        }
        
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
            Assert.Single(newEvents);
            var goalEvent = newEvents.First().StatMap[StatType.GoalsScored].First();
            Assert.Equal(PlayerEvent.TeamType.Away, goalEvent.Team);
            Assert.Equal(TestBuilder.PlayerId, goalEvent.PlayerId);
            
            var context = state.GetGameweekLeagueContext(TestBuilder.LeagueId);
            
            var formattedEvents = GameweekEventsFormatter.FormatNewFixtureEvents(newEvents.ToList(), context);
            foreach (var formatttedEvent in formattedEvents)
            {
                _helper.WriteLine(formatttedEvent);
            }
            Assert.Contains("PlayerFirstName PlayerSecondName scored a goal", formattedEvents.First());
        }
        
        [Theory]
        [InlineData("Kohn Jorsnes", "kors", "Magnus Carlsen", "Magnus Carlsen")]
        [InlineData("Magnus Carlsen", "carlsen", "Magnus Carlsen", "carlsen")]
        [InlineData("Magnus", "carlsen", "Magnus Carlsen", "carlsen")]
        [InlineData("Carlsen", "carlsen", "Magnus Carlsen", "carlsen")]
        [InlineData(null, "carlsen", "Magnus Carlsen", "Magnus Carlsen")]
        public async Task ProducesCorrectTauntString(string slackUserRealName, string slackUserHandle, string entryName, string expectedTauntName)
        {
            // Arrange
            var state = CreateGoalScoredScenario(entryName:entryName, slackUserHandle: slackUserHandle, slackUserRealName:slackUserRealName);
            await state.Reset(1);
            var newEvents = await state.Refresh(1);
            var context = state.GetGameweekLeagueContext(TestBuilder.LeagueId);
            
            Assert.NotEmpty(context.Users);

            // Act
            var formattedEvents = GameweekEventsFormatter.FormatNewFixtureEvents(newEvents.ToList(), context);
            foreach (var formatttedEvent in formattedEvents)
            {
                _helper.WriteLine(formatttedEvent);
            }

            // Assert
            var formattedEvent = formattedEvents.First();
            var regex = new Regex("\\{0\\}.*");
            CustomAssert.AnyOfContains(Constants.EventMessages.TransferredGoalScorerOutTaunts.Select(x => regex.Replace(x, string.Empty)), formattedEvent);
            Assert.Contains(expectedTauntName, formattedEvent);
        }

        private static IState CreateAllMockState()
        {
            return new State(A.Fake<IFixtureClient>(),
                A.Fake<IPlayerClient>(),
                A.Fake<ITeamsClient>(),
                A.Fake<ISlackTeamRepository>(),
                A.Fake<ITransfersByGameWeek>(),
                A.Fake<ISlackClientService>(),
                FakeLogger());
        }
        
        private static IState CreateGoalScoredScenario(string entryName = null, string slackUserRealName = null, string slackUserHandle = null)
        {
            var playerClient = A.Fake<IPlayerClient>();
            A.CallTo(() => playerClient.GetAllPlayers()).Returns(new List<Player>
            {
                TestBuilder.Player()
            });
            
            var slackTeamRepository = A.Fake<ISlackTeamRepository>();
            A.CallTo(() => slackTeamRepository.GetAllTeamsAsync()).Returns(new List<SlackTeam>
            {
                TestBuilder.SlackTeam()
            });
            
            var transfersByGameWeek = A.Fake<ITransfersByGameWeek>();
            A.CallTo(() => transfersByGameWeek.GetTransfersByGameweek(1, 111)).Returns(new List<TransfersByGameWeek.Transfer>
            {
                new TransfersByGameWeek.Transfer
                {
                    EntryId = 2,
                    EntryName = entryName,
                    PlayerTransferredOut = TestBuilder.PlayerId
                }
            });
            
            var teamsClient = A.Fake<ITeamsClient>();
            A.CallTo(() => teamsClient.GetAllTeams()).Returns(new List<Team>
            {
               TestBuilder.HomeTeam(),
               TestBuilder.AwayTeam()
            });


            var fixtureClient = A.Fake<IFixtureClient>();
            A.CallTo(() => fixtureClient.GetFixturesByGameweek(1)).Returns(new List<Fixture>
            {
                TestBuilder.AwayTeamGoal(888, 1)
            }).Once()
                .Then.Returns(
                new List<Fixture>
                {
                    TestBuilder.AwayTeamGoal(888, 2)
                });


            var slackClientService = A.Fake<ISlackClientService>();

            var fakeSlackClient = A.Fake<ISlackClient>();
            A.CallTo(() => fakeSlackClient.UsersList()).Returns(new UsersListResponse
            {
                Ok = true,
                Members = new[]
                {
                    new User
                    {
                        Real_name = slackUserRealName,
                        Name = slackUserHandle
                    }
                }
            });
            A.CallTo(() => slackClientService.CreateClient(A<string>._)).Returns(fakeSlackClient);
            return new State(fixtureClient,
                playerClient,
                teamsClient,
                slackTeamRepository,
                transfersByGameWeek,
                slackClientService,
                FakeLogger());
        }

        private static ILogger<State> FakeLogger()
        {
            return new Logger<State>(A.Fake<ILoggerFactory>());
        }
    }
}