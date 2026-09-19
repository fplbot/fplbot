using FplBot.Data;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests.E2E;

[Collection("App")]
public class ChannelDeliveryFailedHandlerTests(AppFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Day0 = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public async ValueTask InitializeAsync() => await fixture.FlushRedisAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task DiscordFailure_IncrementsCounters()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;

        await fixture.Bus.Publish(new DiscordChannelDeliveryFailed(guild.Id, channelId, "50001", Day0),
            TestContext.Current.CancellationToken);

        var sub = await WaitForFailureCount(() => fixture.GuildRepo.GetChannelSubscription(guild.Id, channelId), 1);
        Assert.Equal(Day0, sub.FailingSince);
    }

    [Fact]
    public async Task DiscordFailuresPastBothThresholds_RemovesSubscription()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;

        for (var day = 0; day < 5; day++)
        {
            var consumedBefore = fixture.ConsumedSoFar;
            await fixture.Bus.Publish(new DiscordChannelDeliveryFailed(guild.Id, channelId, "50001", Day0.AddDays(day * 2)),
                TestContext.Current.CancellationToken);
            await fixture.WaitUntilBusIdle(consumedBefore);
        }

        await AppFixture.WaitUntil(async () => await fixture.GuildRepo.GetChannelSubscription(guild.Id, channelId) is null);
    }

    [Fact]
    public async Task DiscordFailures_LeaveSiblingChannelsUntouched()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var failingChannel = guild.ChannelSubscriptions.First().ChannelId;
        var healthyChannel = "healthy-" + Guid.NewGuid().ToString("N");
        guild.Subscribe(healthyChannel, [FplEvent.PriceChanges]);
        await fixture.GuildRepo.Save(guild);

        for (var day = 0; day < 5; day++)
        {
            var consumedBefore = fixture.ConsumedSoFar;
            await fixture.Bus.Publish(new DiscordChannelDeliveryFailed(guild.Id, failingChannel, "50001", Day0.AddDays(day * 2)),
                TestContext.Current.CancellationToken);
            await fixture.WaitUntilBusIdle(consumedBefore);
        }

        await AppFixture.WaitUntil(async () => await fixture.GuildRepo.GetChannelSubscription(guild.Id, failingChannel) is null);

        var healthy = await fixture.GuildRepo.GetChannelSubscription(guild.Id, healthyChannel);
        Assert.NotNull(healthy);
        Assert.Equal(0, healthy.FailureCount);
    }

    [Fact]
    public async Task ConcurrentFailuresOnDifferentChannelsOfSameGuild_BothRecordedWithoutClobbering()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelA = guild.ChannelSubscriptions.First().ChannelId;
        var channelB = "sibling-" + Guid.NewGuid().ToString("N");
        guild.Subscribe(channelB, [FplEvent.PriceChanges]);
        await fixture.GuildRepo.Save(guild);

        const int failuresPerChannel = 5;

        for (var i = 0; i < failuresPerChannel; i++)
        {
            var consumedBefore = fixture.ConsumedSoFar;
            await Task.WhenAll(
                fixture.Bus.Publish(new DiscordChannelDeliveryFailed(guild.Id, channelA, "50001", Day0), TestContext.Current.CancellationToken),
                fixture.Bus.Publish(new DiscordChannelDeliveryFailed(guild.Id, channelB, "50001", Day0), TestContext.Current.CancellationToken));
            await fixture.WaitUntilBusIdle(consumedBefore, published: 2);
        }

        var subA = await WaitForFailureCount(() => fixture.GuildRepo.GetChannelSubscription(guild.Id, channelA), failuresPerChannel);
        var subB = await WaitForFailureCount(() => fixture.GuildRepo.GetChannelSubscription(guild.Id, channelB), failuresPerChannel);

        Assert.Equal(failuresPerChannel, subA.FailureCount);
        Assert.Equal(failuresPerChannel, subB.FailureCount);
    }

    [Fact]
    public async Task ConcurrentRemovalAndSiblingAccrual_SiblingNotReverted()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var removingChannel = guild.ChannelSubscriptions.First().ChannelId;
        var survivingChannel = "sibling-" + Guid.NewGuid().ToString("N");
        guild.Subscribe(survivingChannel, [FplEvent.PriceChanges]);
        await fixture.GuildRepo.Save(guild);

        const int rounds = 5;

        for (var day = 0; day < rounds; day++)
        {
            var consumedBefore = fixture.ConsumedSoFar;
            await Task.WhenAll(
                fixture.Bus.Publish(new DiscordChannelDeliveryFailed(guild.Id, removingChannel, "50001", Day0.AddDays(day * 2)),
                    TestContext.Current.CancellationToken),
                fixture.Bus.Publish(new DiscordChannelDeliveryFailed(guild.Id, survivingChannel, "50001", Day0),
                    TestContext.Current.CancellationToken));
            await fixture.WaitUntilBusIdle(consumedBefore, published: 2);
        }

        await AppFixture.WaitUntil(async () => await fixture.GuildRepo.GetChannelSubscription(guild.Id, removingChannel) is null);

        var surviving = await WaitForFailureCount(() => fixture.GuildRepo.GetChannelSubscription(guild.Id, survivingChannel), rounds);
        Assert.Equal(rounds, surviving.FailureCount);
    }

    [Fact]
    public async Task SlackFailure_IncrementsCounters()
    {
        var installation = await fixture.SeedInstallation();
        var channelId = installation.ChannelSubscriptions.First().ChannelId;

        await fixture.Bus.Publish(new SlackChannelDeliveryFailed(installation.Id, channelId, "channel_not_found", Day0),
            TestContext.Current.CancellationToken);

        var sub = await WaitForFailureCount(() => fixture.SlackRepo.GetChannelSubscription(installation.Id, channelId), 1);
        Assert.Equal(Day0, sub.FailingSince);
    }

    [Fact]
    public async Task SlackFailuresPastBothThresholds_RemovesSubscription()
    {
        var installation = await fixture.SeedInstallation();
        var channelId = installation.ChannelSubscriptions.First().ChannelId;

        for (var day = 0; day < 5; day++)
        {
            var consumedBefore = fixture.ConsumedSoFar;
            await fixture.Bus.Publish(
                new SlackChannelDeliveryFailed(installation.Id, channelId, "channel_not_found", Day0.AddDays(day * 2)),
                TestContext.Current.CancellationToken);
            await fixture.WaitUntilBusIdle(consumedBefore);
        }

        await AppFixture.WaitUntil(async () => await fixture.SlackRepo.GetChannelSubscription(installation.Id, channelId) is null);
    }

    [Fact]
    public async Task FailureForAlreadyRemovedChannel_IsIgnored()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);

        var consumedBefore = fixture.ConsumedSoFar;
        await fixture.Bus.Publish(new DiscordChannelDeliveryFailed(guild.Id, "no-such-channel", "50001", Day0),
            TestContext.Current.CancellationToken);
        await fixture.WaitUntilBusIdle(consumedBefore);

        var reloaded = await fixture.GuildRepo.GetInstallation(guild.Id);
        Assert.Single(reloaded.ChannelSubscriptions);
    }

    private static async Task<ChannelSubscription> WaitForFailureCount(
        Func<Task<ChannelSubscription?>> read, int expected)
    {
        ChannelSubscription? sub = null;
        await AppFixture.WaitUntil(async () => (sub = await read()) is { } s && HasRecorded(s, expected),
            $"Failure count never reached {expected}");
        return sub!;
    }

    // A failure is two Redis writes - the count, then failingSince - so a read between them sees a
    // counted failure with no date on it. The test wants the state the handler finished writing.
    private static bool HasRecorded(ChannelSubscription sub, int expected) =>
        sub.FailureCount == expected && (expected == 0 || sub.FailingSince is not null);
}
