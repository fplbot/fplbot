using FplBot.Data;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests.E2E.Discord;

[Collection("App")]
public class DiscordFixtureEventsHandlerTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.DiscordCapture.Reset();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task WhenChannelSubscribedToGoals_PostsGoalEvent()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.FixtureGoals]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        await fixture.Bus.Publish(new FixtureEventsOccured([SampleGoalEvent()]), TestContext.Current.CancellationToken);

        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("TestScorer", msg.Description);
    }

    [Fact]
    public async Task WhenChannelNotSubscribedToGoals_DoesNotPostGoalEvent()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        await fixture.Bus.Publish(new FixtureEventsOccured([SampleGoalEvent()]), TestContext.Current.CancellationToken);

        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.DiscordCapture.AnyMessage(channelId));
    }

    private static FixtureEvents SampleGoalEvent() =>
        new(
            new FixtureScore(
                new FixtureTeam(10, "Home Team", "HOM"),
                new FixtureTeam(20, "Away Team", "AWY"),
                Minutes: 30,
                HomeTeamScore: 0,
                AwayTeamScore: 1),
            new Dictionary<StatType, List<PlayerEvent>>
            {
                [StatType.GoalsScored] = [new PlayerEvent(new PlayerDetails(1, "TestScorer"), TeamType.Away, IsRemoved: false)]
            });
}
