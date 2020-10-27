using System;
using System.Linq;
using Slackbot.Net.Extensions.FplBot.Abstractions;
using Slackbot.Net.Extensions.FplBot.Extensions;
using Xunit;

namespace FplBot.Tests
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("fixtureassists, bøler", new []{EventSubscription.FixtureAssists}, new []{"bøler"})]
        [InlineData("fixtureassists, fixturegoals", new []{EventSubscription.FixtureAssists, EventSubscription.FixtureGoals}, null)]
        [InlineData("fixtureassists, fixturegoals, all, cardss", new []{EventSubscription.FixtureAssists, EventSubscription.FixtureGoals, EventSubscription.All}, new []{"cardss"})]
        public void ParseSubscriptionString_ShouldWork(string input, EventSubscription[] expected, string[] unableToParse) 
        {
            // Act
            var result = input.ParseSubscriptionString(",", out var errors);

            // Assert
            Assert.True(result.All(expected.Contains));
            Assert.True(unableToParse == null && !errors.Any() || errors.All(unableToParse.Contains));
        }
    }
}