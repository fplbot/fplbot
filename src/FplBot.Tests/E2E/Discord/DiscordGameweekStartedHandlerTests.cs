using FplBot.Data;
using FplBot.Messaging.Contracts.Commands.v1;

namespace FplBot.Tests.E2E.Discord;

[Collection("App")]
public class DiscordGameweekStartedHandlerTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.DiscordCapture.Reset();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task WhenChannelSubscribedToCaptainsWithLeague_PostsGameweekStartedMessage()
    {
        var installedGuild = await fixture.SeedGuildInstallation(54321, [EventSubscription.Captains]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        await fixture.Bus.Publish(new ProcessGameweekStartedForGuildChannel(installedGuild.Id, channelId, 5),
            TestContext.Current.CancellationToken);

        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("Gameweek 5!", msg.Title);
    }

    [Fact]
    public async Task WhenChannelNotSubscribedToCaptainsOrTransfers_DoesNotPost()
    {
        var installedGuild = await fixture.SeedGuildInstallation(54321, [EventSubscription.PriceChanges]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        await fixture.Bus.Publish(new ProcessGameweekStartedForGuildChannel(installedGuild.Id, channelId, 5),
            TestContext.Current.CancellationToken);

        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.DiscordCapture.AnyMessage(channelId));
    }
}
