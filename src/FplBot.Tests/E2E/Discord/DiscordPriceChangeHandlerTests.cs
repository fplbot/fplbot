using FplBot.Data;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests.E2E.Discord;

[Collection("App")]
public class DiscordPriceChangeHandlerTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.DiscordCapture.Reset();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task WhenChannelSubscribedToPriceChanges_PostsFormattedPriceChange()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        await fixture.Bus.Publish(new PlayersPriceChanged([
            new PlayerWithPriceChange(
                PlayerId: 1,
                WebName: "Haaland",
                CostChangeEvent: 1,
                NowCost: 145,
                OwnershipPercentage: 30.5,
                TeamId: 11,
                TeamShortName: "MCI")
        ]), TestContext.Current.CancellationToken);

        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("Haaland", msg.Description);
    }

    [Fact]
    public async Task WhenChannelNotSubscribedToPriceChanges_DoesNotPost()
    {
        await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.InjuryUpdates]);

        await fixture.Bus.Publish(new PlayersPriceChanged([
            new PlayerWithPriceChange(
                PlayerId: 1,
                WebName: "Haaland",
                CostChangeEvent: 1,
                NowCost: 145,
                OwnershipPercentage: 30.5,
                TeamId: 11,
                TeamShortName: "MCI")
        ]), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.DiscordCapture.WaitForMessageAsync(TimeSpan.FromMilliseconds(500)));
    }
}
