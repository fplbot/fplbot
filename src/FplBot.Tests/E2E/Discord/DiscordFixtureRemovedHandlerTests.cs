using FplBot.Data;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests.E2E.Discord;

[Collection("App")]
public class DiscordFixtureRemovedHandlerTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.DiscordCapture.Reset();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task WhenChannelSubscribedToFixtureRemoved_PostsNotice()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.FixtureRemovedFromGameweek]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        await fixture.Bus.Publish(new FixtureRemovedFromGameweek(5,
                new RemovedFixture(1, new RemovedTeam(1, "Home Team", "HOM"), new RemovedTeam(2, "Away Team", "AWY"))),
            TestContext.Current.CancellationToken);

        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("Fixture off!", msg.Title);
        Assert.Contains("Home Team-Away Team", msg.Description);
    }

    [Fact]
    public async Task WhenChannelNotSubscribedToFixtureRemoved_DoesNotPost()
    {
        await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);

        await fixture.Bus.Publish(new FixtureRemovedFromGameweek(5,
                new RemovedFixture(1, new RemovedTeam(1, "Home Team", "HOM"), new RemovedTeam(2, "Away Team", "AWY"))),
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.DiscordCapture.WaitForMessageAsync(TimeSpan.FromMilliseconds(500)));
    }
}
