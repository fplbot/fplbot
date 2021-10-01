using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Slack.Abstractions;
using FplBot.Slack.Data.Models;
using FplBot.Slack.Helpers;
using FplBot.Slack.Helpers.Formatting.FixtureStats;
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
        [InlineData("Magnus Carlsen", "carlsen", "Magnus Carlsen", "<@U123>")]
        [InlineData("Magnus", "carlsen", "Magnus Carlsen", "<@U123>")]
        [InlineData("Carlsen", "carlsen", "Magnus Carlsen", "<@U123>")]
        [InlineData(null, "carlsen", "Magnus Carlsen", "Magnus Carlsen")]
        public void ProducesCorrectTauntString(string slackUserRealName, string slackUserHandle, string entryName, string expectedTauntName)
        {
            // Arrange


            // Act
            var formattedEvents = GameweekEventsFormatter.FormatNewFixtureEvents(CreateGoalEvent(), new [] { EventSubscription.All }, CreateTransferOutForGoalScorerContext(slackUserRealName, slackUserHandle, entryName));
            foreach (var formatttedEvent in formattedEvents)
            {
                _helper.WriteLine(formatttedEvent);
            }
            // Assert
            var formattedEvent = formattedEvents.First();
            var regex = new Regex("\\{0\\}.*");
            CustomAssert.AnyOfContains(GoalFormatter.GoalJokes.Select(x => regex.Replace(x, string.Empty)), formattedEvent);
            Assert.Contains(expectedTauntName, formattedEvent);

        }

        [Fact]
        public void RegularGoalScored()
        {
            var formattedEvents = GameweekEventsFormatter.FormatNewFixtureEvents(CreateGoalEvent(), new [] { EventSubscription.All }, CreateNoTransfersForGoalScorer());
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
                (
                    new FixtureScore
                    (
                        new FixtureTeam(fixture.HomeTeamId, TestBuilder.HomeTeam().Name, TestBuilder.HomeTeam().ShortName),
                        new FixtureTeam(fixture.AwayTeamId, TestBuilder.AwayTeam().Name, TestBuilder.AwayTeam().ShortName),
                        1,
                        0
                    ),
                    new Dictionary<StatType, List<PlayerEvent>>
                    {
                        { StatType.GoalsScored, new List<PlayerEvent>{ new PlayerEvent(TestBuilder.PlayerDetails(), TeamType.Home, false)}}
                    }
                )
            };
        }

        private TauntData CreateTransferOutForGoalScorerContext(string slackUserRealName, string slackUserHandle, string entryName)
        {
            return new TauntData(
                new[]
                {
                    new TransfersByGameWeek.Transfer
                    {
                        EntryId = 2, EntryName = entryName, PlayerTransferredOut = TestBuilder.PlayerId
                    }
                },
                Array.Empty<GameweekEntry>(),
                new[]
                {
                    new User { Id = "U123", Real_name = slackUserRealName, Name = slackUserHandle }
                });

        }

        private TauntData CreateNoTransfersForGoalScorer()
        {
            return new TauntData(
                Array.Empty<TransfersByGameWeek.Transfer>(),
                Array.Empty<GameweekEntry>(),
                new[]
                {
                    new User {Real_name = "dontCare dontCaresen", Name = "dontCareName"}
                });
        }
    }
}
