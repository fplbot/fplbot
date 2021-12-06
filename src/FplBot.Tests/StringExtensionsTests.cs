using FplBot.Slack.Data;
using FplBot.Slack.Data.Models;

namespace FplBot.Tests;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("fixtureassists, bøler", new []{EventSubscription.FixtureAssists}, new []{"bøler"})]
    [InlineData("fixtureassists, fixturegoals", new []{EventSubscription.FixtureAssists, EventSubscription.FixtureGoals}, null)]
    [InlineData("fixtureassists, fixturegoals, all, cardss", new []{EventSubscription.FixtureAssists, EventSubscription.FixtureGoals, EventSubscription.All}, new []{"cardss"})]
    public void ParseSubscriptionString_ShouldWork(string input, EventSubscription[] expected, string[] unableToParse)
    {
        // Act
        var (result, errors) = input.ParseSubscriptionString(",");

        // Assert
        Assert.True(expected.All(result.Contains));
        if (unableToParse == null)
        {
            Assert.True(!errors.Any());
        }
        else
        {
            Assert.True(unableToParse.All(errors.Contains));
        }
    }
}
