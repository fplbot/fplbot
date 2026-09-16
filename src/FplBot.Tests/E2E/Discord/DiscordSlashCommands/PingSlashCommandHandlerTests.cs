using FplBot.Data;
using FplBot.Domain;
using FplBot.Tests.Helpers;

namespace FplBot.Tests.E2E.Discord.DiscordSlashCommands;

[Collection("App")]
public class PingSlashCommandHandlerTests(AppFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Day0 = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public async ValueTask InitializeAsync()
    {
        fixture.DiscordCapture.Reset();
        fixture.ResetChannelOutcomes();
        await fixture.FlushRedisAsync();
    }

    public ValueTask DisposeAsync()
    {
        fixture.ResetChannelOutcomes();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Ping_PostsPongToTheChannel()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;

        await fixture.AskDiscord("ping", guildId: guild.Id, channelId: channelId);

        var posted = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.Contains("pong", posted.Text ?? posted.Description ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SuccessfulPing_ClearsFailureCounters()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;
        guild.GetChannel(channelId)!.RecordDeliveryFailure(Day0);
        guild.GetChannel(channelId)!.RecordDeliveryFailure(Day0.AddDays(1));
        await fixture.GuildRepo.Save(guild);

        await fixture.AskDiscord("ping", guildId: guild.Id, channelId: channelId);
        await fixture.DiscordCapture.WaitForMessageAsync(channelId);

        var sub = await fixture.GuildRepo.GetChannelSubscription(guild.Id, channelId);
        Assert.Equal(0, sub!.FailureCount);
        Assert.Null(sub.FailingSince);
    }

    [Fact]
    public async Task FailedPing_LeavesCountersUntouched()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;
        guild.GetChannel(channelId)!.RecordDeliveryFailure(Day0);
        guild.GetChannel(channelId)!.RecordDeliveryFailure(Day0.AddDays(1));
        await fixture.GuildRepo.Save(guild);
        fixture.DiscordChannelFails(channelId, 50013);

        var response = await fixture.AskDiscord("ping", guildId: guild.Id, channelId: channelId);
        await Task.Delay(500, TestContext.Current.CancellationToken);

        var sub = await fixture.GuildRepo.GetChannelSubscription(guild.Id, channelId);
        Assert.Equal(2, sub!.FailureCount);
        Assert.Equal(Day0, sub.FailingSince);
        Assert.Contains("permission", response.EmbedDescription(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FailedPingOnUnknownChannel_PointsAtVisibility()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;
        fixture.DiscordChannelFails(channelId, 10003);

        var response = await fixture.AskDiscord("ping", guildId: guild.Id, channelId: channelId);

        Assert.Contains("can't see it", response.EmbedDescription(), StringComparison.OrdinalIgnoreCase);
    }
}
