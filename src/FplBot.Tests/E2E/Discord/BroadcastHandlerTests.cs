using FplBot.Data;
using FplBot.Messaging.Contracts.Commands.v1;

namespace FplBot.Tests.E2E.Discord;

[Collection("App")]
public class BroadcastHandlerTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.DiscordCapture.Reset();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task AllChannels_ChannelSubscribedToCaptains_ReceivesBroadcast()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Captains]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        await fixture.Bus.Publish(new BroadcastToDiscord("Hello guilds!", ChannelFilter.AllChannels),
            TestContext.Current.CancellationToken);

        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("Hello guilds!", msg.Text);
    }

    [Fact]
    public async Task AllChannels_ChannelNotSubscribedToBroadcastEligibleEvents_DoesNotReceiveBroadcast()
    {
        await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);

        await fixture.Bus.Publish(new BroadcastToDiscord("Hello guilds!", ChannelFilter.AllChannels),
            TestContext.Current.CancellationToken);

        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.DiscordCapture.AnyMessage());
    }

    [Fact]
    public async Task FilterNotSet_DoesNotBroadcastAtAll()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Captains]);

        await fixture.Bus.Publish(new BroadcastToDiscord("Hello guilds!", ChannelFilter.NotSet),
            TestContext.Current.CancellationToken);

        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.DiscordCapture.AnyMessage(installedGuild.ChannelSubscriptions.First().ChannelId));
    }
}
