using FplBot.Data;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests.E2E.Discord;

[Collection("App")]
public class DiscordNearDeadlineHandlerTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.DiscordCapture.Reset();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task OneHourToDeadline_ChannelSubscribedToDeadlines_PostsWarning()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Deadlines]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        await fixture.Bus.Publish(new OneHourToDeadline(new GameweekNearingDeadline(5, "Gameweek 5", DateTime.UtcNow)),
            TestContext.Current.CancellationToken);

        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("60 minutes", msg.Text);
    }

    [Fact]
    public async Task TwentyFourHoursToDeadline_ChannelSubscribedToDeadlines_PostsWarning()
    {
        var installedGuild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.Deadlines]);
        var channelId = installedGuild.ChannelSubscriptions.First().ChannelId;

        await fixture.Bus.Publish(new TwentyFourHoursToDeadline(new GameweekNearingDeadline(5, "Gameweek 5", DateTime.UtcNow)),
            TestContext.Current.CancellationToken);

        var msg = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("24 hours", msg.Text);
    }

    [Fact]
    public async Task WhenChannelNotSubscribedToDeadlines_DoesNotPost()
    {
        await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);

        await fixture.Bus.Publish(new OneHourToDeadline(new GameweekNearingDeadline(5, "Gameweek 5", DateTime.UtcNow)),
            TestContext.Current.CancellationToken);

        await fixture.WaitUntilBusIdle();
        Assert.False(fixture.DiscordCapture.AnyMessage());
    }
}
