using FplBot.Data;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests.E2E.Discord;

[Collection("App")]
public class DiscordInjuryUpdateHandlerTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.DiscordCapture.Reset();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task WhenChannelSubscribedToInjuryUpdates_PostsFormattedUpdate()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.InjuryUpdates]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        await fixture.Bus.Publish(new InjuryUpdateOccured([
            new InjuredPlayerUpdate(
                new InjuredPlayer(1, "Salah", 25.0, new TeamDescription(14, "LIV", "Liverpool")),
                new InjuryStatus("a", ""),
                new InjuryStatus("d", "Knee injury"))
        ]), TestContext.Current.CancellationToken);

        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("Salah", msg.Description);
    }

    [Fact]
    public async Task WhenChannelNotSubscribedToInjuryUpdates_DoesNotPost()
    {
        await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);

        await fixture.Bus.Publish(new InjuryUpdateOccured([
            new InjuredPlayerUpdate(
                new InjuredPlayer(1, "Salah", 25.0, new TeamDescription(14, "LIV", "Liverpool")),
                new InjuryStatus("a", ""),
                new InjuryStatus("d", "Knee injury"))
        ]), TestContext.Current.CancellationToken);

        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.DiscordCapture.AnyMessage());
    }
}
