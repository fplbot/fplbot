using System.Net;
using FplBot.Data;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests.E2E.Discord;

[Collection("App")]
public class DiscordDeliveryFailureTests(AppFixture fixture) : IAsyncLifetime
{
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
    public async Task ChannelScopedFailure_RecordsFailure()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;
        fixture.DiscordChannelFails(channelId, 50001);

        await PublishPriceChange();

        await WaitForFailureCount(fixture, guild.Id, channelId, 1);
    }

    [Fact]
    public async Task TransientFailure_DoesNotRecordFailure()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;
        fixture.DiscordChannelFails(channelId, HttpStatusCode.TooManyRequests);

        await PublishPriceChange();

        var sub = await fixture.GuildRepo.GetChannelSubscription(guild.Id, channelId);
        Assert.NotNull(sub);
        Assert.Equal(0, sub.FailureCount);
    }

    [Fact]
    public async Task GuildLevelFailure_DoesNotRecordFailure()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;
        fixture.DiscordChannelFails(channelId, 10004);

        await PublishPriceChange();


        var sub = await fixture.GuildRepo.GetChannelSubscription(guild.Id, channelId);
        Assert.NotNull(sub);
        Assert.Equal(0, sub.FailureCount);
    }

    private Task PublishPriceChange() =>
        fixture.Bus.Publish(new PlayersPriceChanged([
            new PlayerWithPriceChange(
                PlayerId: 1, WebName: "Haaland", CostChangeEvent: 1, NowCost: 145,
                OwnershipPercentage: 30.5, TeamId: 11, TeamShortName: "MCI")
        ]), TestContext.Current.CancellationToken);

    internal static async Task WaitForFailureCount(AppFixture fixture, string guildId, string channelId, int expected)
    {
        await AppFixture.WaitUntil(
            async () => await fixture.GuildRepo.GetChannelSubscription(guildId, channelId) is { } sub
                        && sub.FailureCount == expected && (expected == 0 || sub.FailingSince is not null),
            $"Failure count never reached {expected}");
    }
}
