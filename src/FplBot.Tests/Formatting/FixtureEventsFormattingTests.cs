using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FplBot.Formatting;
using FplBot.Formatting.FixtureStats;
using FplBot.Messaging.Contracts.Events.v1;
using FplBot.Slack.Helpers;
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
            var formattedEvents = GameweekEventsFormatter.FormatNewFixtureEvents(CreateGoalEvent(), subscribes => true,FormattingType.Slack, CreateTransferOutForGoalScorerContext(slackUserRealName, slackUserHandle, entryName));
            foreach (var formatttedEvent in formattedEvents)
            {
                _helper.WriteLine($"{formatttedEvent.Title} {formatttedEvent.Details}");
            }
            // Assert
            var formattedEvent = formattedEvents.First();
            var regex = new Regex("\\{0\\}.*");
            CustomAssert.AnyOfContains(GoalDescriber.GoalJokes.Select(x => regex.Replace(x, string.Empty)), formattedEvent.Details);
            Assert.Contains(expectedTauntName, formattedEvent.Details);

        }

        [Fact]
        public void RegularGoalScored()
        {
            var formattedEvents = GameweekEventsFormatter.FormatNewFixtureEvents(CreateGoalEvent(), subscribes => true, FormattingType.Slack, CreateNoTransfersForGoalScorer());
            foreach (var formatttedEvent in formattedEvents)
            {
                _helper.WriteLine($"{formatttedEvent.Title} {formatttedEvent.Details}");
            }
            Assert.Contains("PlayerFirstName PlayerSecondName scored a goal", formattedEvents.First().Details);
        }

        [Fact]
        public void VAR_Slack()
        {
            FormattingType formattingType = FormattingType.Slack;
            var formattedEvents = GameweekEventsFormatter.FormatNewFixtureEvents(CreateGoalEvent(removed:true), subscribes => true, formattingType, CreateNoTransfersForGoalScorer());
            foreach (var formatttedEvent in formattedEvents)
            {
                _helper.WriteLine($"{formatttedEvent.Title} {formatttedEvent.Details}");
            }
            Assert.Contains("~PlayerFirstName PlayerSecondName scored a goal! ⚽️~ (VAR? 🤷‍♀️)", formattedEvents.First().Details);
        }

        [Fact]
        public void VAR_Discord()
        {
            FormattingType formattingType = FormattingType.Discord;
            var formattedEvents = GameweekEventsFormatter.FormatNewFixtureEvents(CreateGoalEvent(removed:true), subscribes => true, formattingType, CreateNoTransfersForGoalScorer());
            foreach (var formatttedEvent in formattedEvents)
            {
                _helper.WriteLine($"{formatttedEvent.Title} {string.Join("\n", formatttedEvent.Details)}");
            }
            Assert.Contains("~~PlayerFirstName PlayerSecondName scored a goal! ⚽️~~ (VAR? 🤷‍♀️)", formattedEvents.First().Details);
        }

        [Fact]
        public void Formatting_ShouldNotFormat_ForIrrelevantStats()
        {
            var disordEvents = GameweekEventsFormatter.FormatNewFixtureEvents(CreateIrrelevantEvents(), subscribes => true,  FormattingType.Discord);
            foreach (var formatttedEvent in disordEvents)
            {
                _helper.WriteLine($"{formatttedEvent.Title}\n {string.Join("\n", formatttedEvent.Details)}");
            }
            Assert.Single(disordEvents);
            Assert.Contains("scored a goal", disordEvents.First().Details);


            var slackEvents = GameweekEventsFormatter.FormatNewFixtureEvents(CreateIrrelevantEvents(), subscribes => true, FormattingType.Slack);
            foreach (var formatttedEvent in slackEvents)
            {
                _helper.WriteLine($"{formatttedEvent.Title}\n {string.Join("\n", formatttedEvent.Details)}");
            }
            Assert.Single(slackEvents);
            Assert.Contains("scored a goal", slackEvents.First().Details);
        }

        private List<FixtureEvents> CreateIrrelevantEvents()
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
                        { StatType.YellowCards, new List<PlayerEvent>{ new PlayerEvent(TestBuilder.PlayerDetails(), TeamType.Home, IsRemoved:false)}},
                        { StatType.Bonus, new List<PlayerEvent>{ new PlayerEvent(TestBuilder.PlayerDetails(), TeamType.Home, IsRemoved:false)}},
                        { StatType.Saves, new List<PlayerEvent>{ new PlayerEvent(TestBuilder.PlayerDetails(), TeamType.Home, IsRemoved:false)}},
                        { StatType.GoalsScored, new List<PlayerEvent>{ new PlayerEvent(TestBuilder.PlayerDetails(), TeamType.Home, IsRemoved:false)}},

                    }
                )
            };
        }

        private List<FixtureEvents> CreateGoalEvent(bool removed = false)
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
                        { StatType.GoalsScored, new List<PlayerEvent>{ new PlayerEvent(TestBuilder.PlayerDetails(), TeamType.Home, IsRemoved:removed)}}
                    }
                )
            };
        }

        private TauntData CreateTransferOutForGoalScorerContext(string slackUserRealName, string slackUserHandle, string entryName)
        {
            User[] users = new[]
            {
                new User { Id = "U123", Real_name = slackUserRealName, Name = slackUserHandle }
            };
            return new TauntData(
                new[]
                {
                    new TransfersByGameWeek.Transfer
                    {
                        EntryId = 2, EntryName = entryName, PlayerTransferredOut = TestBuilder.PlayerId
                    }
                },
                Array.Empty<GameweekEntry>(),
                entry => SlackHandleHelper.GetSlackHandleOrFallback(users, entry));
        }

        private TauntData CreateNoTransfersForGoalScorer()
        {
            User[] users = new[]
            {
                new User {Real_name = "dontCare dontCaresen", Name = "dontCareName"}
            };
            return new TauntData(
                Array.Empty<TransfersByGameWeek.Transfer>(),
                Array.Empty<GameweekEntry>(),
                entry => SlackHandleHelper.GetSlackHandleOrFallback(users, entry));
        }
    }
}
