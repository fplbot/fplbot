using FplBot.Data;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests.E2E.Discord;

[Collection("App")]
public class DiscordLineupReadyHandlerTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.DiscordCapture.Reset();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task WhenChannelSubscribedToLineups_PostsFormattedLineup()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Lineups]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        await fixture.Bus.Publish(new LineupReady(SampleLineups()), TestContext.Current.CancellationToken);

        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("Lineups", msg.Title);
        Assert.Contains("HomeTeam", msg.Title);
    }

    [Fact]
    public async Task WhenChannelNotSubscribedToLineups_DoesNotPost()
    {
        await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);

        await fixture.Bus.Publish(new LineupReady(SampleLineups()), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.DiscordCapture.WaitForMessageAsync(TimeSpan.FromMilliseconds(500)));
    }

    private static Lineups SampleLineups() =>
        new(1,
            new FormationDetails("HomeTeam", "4-4-2", []),
            new FormationDetails("AwayTeam", "4-3-3", []));
}
