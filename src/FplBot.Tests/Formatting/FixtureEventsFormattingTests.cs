using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Fpl.Client.Models;
using FplBot.Core.Abstractions;
using FplBot.Core.Helpers;
using FplBot.Core.Models;
using FplBot.Core.Taunts;
using FplBot.Tests.Helpers;
using Slackbot.Net.SlackClients.Http.Models.Responses.UsersList;
using Xunit;
using Xunit.Abstractions;

namespace FplBot.Tests
{
    public class FixtureEventsFormattingTests
    {
        private readonly ITestOutputHelper _helper;

        public FixtureEventsFormattingTests(ITestOutputHelper helper)
        {
            _helper = helper;
        }
        
        [Theory]
        [InlineData("Kohn Jorsnes", "kors", "Magnus Carlsen", "Magnus Carlsen")]
        [InlineData("Magnus Carlsen", "carlsen", "Magnus Carlsen", "carlsen")]
        [InlineData("Magnus", "carlsen", "Magnus Carlsen", "carlsen")]
        [InlineData("Carlsen", "carlsen", "Magnus Carlsen", "carlsen")]
        [InlineData(null, "carlsen", "Magnus Carlsen", "Magnus Carlsen")]
        public void ProducesCorrectTauntString(string slackUserRealName, string slackUserHandle, string entryName, string expectedTauntName)
        {
            // Arrange
     
        
            // Act
            var formattedEvents = GameweekEventsFormatter.FormatNewFixtureEvents(CreateGoalEvent(), CreateTransferOutForGoalScorerContext(slackUserRealName, slackUserHandle, entryName));
            foreach (var formatttedEvent in formattedEvents)
            {
                _helper.WriteLine(formatttedEvent);
            }
            // Assert
            var formattedEvent = formattedEvents.First();
            var regex = new Regex("\\{0\\}.*");
            CustomAssert.AnyOfContains(new GoalTaunt().JokePool.Select(x => regex.Replace(x, string.Empty)), formattedEvent);
            Assert.Contains(expectedTauntName, formattedEvent);
            
        }

        [Fact]
        public void RegularGoalScored()
        {
            var formattedEvents = GameweekEventsFormatter.FormatNewFixtureEvents(CreateGoalEvent(), CreateNoTransfersForGoalScorer());
            foreach (var formatttedEvent in formattedEvents)
            {
                _helper.WriteLine(formatttedEvent);
            }
            Assert.Contains("PlayerFirstName PlayerSecondName scored a goal", formattedEvents.First());
        }

        private List<FixtureEvents> CreateGoalEvent()
        {
            var fixture = TestBuilder.AwayTeamGoal(1,1);
            return new List<FixtureEvents>
            {
                new FixtureEvents
                {
                    GameScore = new GameScore
                    {
                        HomeTeamId = fixture.HomeTeamId,
                        AwayTeamId = fixture.AwayTeamId
                    },
                    StatMap = new Dictionary<StatType, List<PlayerEvent>>
                    {
                        { StatType.GoalsScored, new List<PlayerEvent>{ new PlayerEvent(TestBuilder.Player().Id, PlayerEvent.TeamType.Home, false)}}
                    }
                }
            };
        }

        private GameweekLeagueContext CreateTransferOutForGoalScorerContext(string slackUserRealName, string slackUserHandle, string entryName)
        {
            return new GameweekLeagueContext
            {
                Users = new[]
                {
                    new User {Real_name = slackUserRealName, Name = slackUserHandle}
                },
                Players = new List<Player>{ TestBuilder.Player() },
                Teams = new List<Team> { TestBuilder.HomeTeam(), TestBuilder.AwayTeam()},
                SlackTeam = new SlackTeam { Subscriptions = new [] {EventSubscription.All}},
                TransfersForLeague = new []
                {
                    new TransfersByGameWeek.Transfer
                    {
                        EntryId = 2,
                        EntryName = entryName,
                        PlayerTransferredOut = TestBuilder.PlayerId
                    }
                }
            };
        }
        
        private GameweekLeagueContext CreateNoTransfersForGoalScorer()
        {
            return new GameweekLeagueContext
            {
                Users = new[]
                {
                    new User {Real_name = "dontCare dontCaresen", Name = "dontCareName"}
                },
                Players = new List<Player>{ TestBuilder.Player() },
                Teams = new List<Team> { TestBuilder.HomeTeam(), TestBuilder.AwayTeam()},
                SlackTeam = new SlackTeam { Subscriptions = new [] {EventSubscription.All}},
                TransfersForLeague = new List<TransfersByGameWeek.Transfer>()
            };
        }
    }
}