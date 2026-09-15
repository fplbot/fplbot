using FplBot.Data;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests.E2E.Discord;

[Collection("App")]
public class DiscordNewPlayersHandlerTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.DiscordCapture.Reset();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task NewPlayersRegistered_ChannelSubscribedToNewPlayers_PostsFormattedList()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.NewPlayers]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        await fixture.Bus.Publish(new NewPlayersRegistered([
            new NewPlayer(1, "Haaland", 145, 11, "MCI")
        ]), TestContext.Current.CancellationToken);

        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("Haaland", msg.Description);
    }

    [Fact]
    public async Task NewPlayersRegistered_ChannelNotSubscribedToNewPlayers_DoesNotPost()
    {
        await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);

        await fixture.Bus.Publish(new NewPlayersRegistered([
            new NewPlayer(1, "Haaland", 145, 11, "MCI")
        ]), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.DiscordCapture.WaitForMessageAsync(TimeSpan.FromMilliseconds(500)));
    }

    [Fact]
    public async Task PremiershipPlayerTransferred_ChannelSubscribedToNewPlayers_PostsTransfer()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.NewPlayers]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        await fixture.Bus.Publish(new PremiershipPlayerTransferred([
            new InternalPremiershipTransfer("Haaland", "Dortmund", "Man City")
        ]), TestContext.Current.CancellationToken);

        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("Haaland", msg.Description);
    }

    [Fact]
    public async Task PremiershipPlayerTransferred_ChannelNotSubscribedToNewPlayers_DoesNotPost()
    {
        await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);

        await fixture.Bus.Publish(new PremiershipPlayerTransferred([
            new InternalPremiershipTransfer("Haaland", "Dortmund", "Man City")
        ]), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            fixture.DiscordCapture.WaitForMessageAsync(TimeSpan.FromMilliseconds(500)));
    }
}
