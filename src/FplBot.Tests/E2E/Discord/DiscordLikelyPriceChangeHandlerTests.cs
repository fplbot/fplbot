using FplBot.Data;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests.E2E.Discord;

[Collection("App")]
public class DiscordLikelyPriceChangeHandlerTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.DiscordCapture.Reset();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task WhenChannelSubscribedToLikelyPriceChanges_PostsFormattedLikelyPriceChange()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.LikelyPriceChanges]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        await fixture.Bus.Publish(new PlayersLikelyToChangePrice([
            new PlayerLikelyPriceChange(1, "Haaland", 145, 11, "MCI", "123.2", 5)
        ]), TestContext.Current.CancellationToken);

        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("Haaland", msg.Description);
    }

    [Fact]
    public async Task WhenChannelSubscribedToPriceChangesOnly_StillPostsAsShowcase()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        await fixture.Bus.Publish(new PlayersLikelyToChangePrice([
            new PlayerLikelyPriceChange(1, "Haaland", 145, 11, "MCI", "123.2", 5)
        ]), TestContext.Current.CancellationToken);

        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("Haaland", msg.Description);
    }

    [Fact]
    public async Task WhenChannelNotSubscribedToLikelyPriceChangesOrPriceChanges_DoesNotPost()
    {
        await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.InjuryUpdates]);

        await fixture.Bus.Publish(new PlayersLikelyToChangePrice([
            new PlayerLikelyPriceChange(1, "Haaland", 145, 11, "MCI", "123.2", 5)
        ]), TestContext.Current.CancellationToken);

        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.DiscordCapture.AnyMessage());
    }
}
