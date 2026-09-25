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

        await fixture.Bus.Publish(new DiscordChannelDeliveryFailed(guild.ExternalId, channelId, "50001", Day0),
            TestContext.Current.CancellationToken);

        var sub = await WaitForFailureCount(() => fixture.GuildRepo.GetChannelSubscription(guild.ExternalId, channelId), 1);
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
            await fixture.Bus.Publish(new DiscordChannelDeliveryFailed(guild.ExternalId, channelId, "50001", Day0.AddDays(day * 2)),
                TestContext.Current.CancellationToken);
            await fixture.WaitUntilBusIdle(consumedBefore);
        }

        await AppFixture.WaitUntil(async () => await fixture.GuildRepo.GetChannelSubscription(guild.ExternalId, channelId) is null);
    }

    [Fact]
    public async Task DiscordFailuresPastBothThresholds_WhenLastSubscriptionPurged_UninstallsGuildAndClearsMemberCount()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;
        await fixture.GuildMemberCountRepo.SetApproximateMemberCount(guild.ExternalId, 500);

        for (var day = 0; day < 5; day++)
        {
            var consumedBefore = fixture.ConsumedSoFar;
            await fixture.Bus.Publish(new DiscordChannelDeliveryFailed(guild.ExternalId, channelId, "50001", Day0.AddDays(day * 2)),
                TestContext.Current.CancellationToken);
            await fixture.WaitUntilBusIdle(consumedBefore);
        }

        await AppFixture.WaitUntil(async () => await fixture.GuildRepo.FindInstallationByTeamId(guild.ExternalId) is null);

        var counts = await fixture.GuildMemberCountRepo.GetAll();
        Assert.False(counts.ContainsKey(guild.ExternalId));
    }

    [Fact]
    public async Task DiscordFailuresPastBothThresholds_WhenSiblingChannelSurvives_GuildStaysInstalled()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var failingChannel = guild.ChannelSubscriptions.First().ChannelId;
        var healthyChannel = "healthy-" + Guid.NewGuid().ToString("N");
        guild.Subscribe(healthyChannel, [FplEvent.PriceChanges]);
        await fixture.GuildRepo.Save(guild);

        for (var day = 0; day < 5; day++)
        {
            var consumedBefore = fixture.ConsumedSoFar;
            await fixture.Bus.Publish(new DiscordChannelDeliveryFailed(guild.ExternalId, failingChannel, "50001", Day0.AddDays(day * 2)),
                TestContext.Current.CancellationToken);
            await fixture.WaitUntilBusIdle(consumedBefore);
        }

        await AppFixture.WaitUntil(async () => await fixture.GuildRepo.GetChannelSubscription(guild.ExternalId, failingChannel) is null);

        Assert.NotNull(await fixture.GuildRepo.FindInstallationByTeamId(guild.ExternalId));
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
            await fixture.Bus.Publish(new DiscordChannelDeliveryFailed(guild.ExternalId, failingChannel, "50001", Day0.AddDays(day * 2)),
                TestContext.Current.CancellationToken);
            await fixture.WaitUntilBusIdle(consumedBefore);
        }

        await AppFixture.WaitUntil(async () => await fixture.GuildRepo.GetChannelSubscription(guild.ExternalId, failingChannel) is null);

        var healthy = await fixture.GuildRepo.GetChannelSubscription(guild.ExternalId, healthyChannel);
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
                fixture.Bus.Publish(new DiscordChannelDeliveryFailed(guild.ExternalId, channelA, "50001", Day0), TestContext.Current.CancellationToken),
                fixture.Bus.Publish(new DiscordChannelDeliveryFailed(guild.ExternalId, channelB, "50001", Day0), TestContext.Current.CancellationToken));
            await fixture.WaitUntilBusIdle(consumedBefore, published: 2);
        }

        var subA = await WaitForFailureCount(() => fixture.GuildRepo.GetChannelSubscription(guild.ExternalId, channelA), failuresPerChannel);
        var subB = await WaitForFailureCount(() => fixture.GuildRepo.GetChannelSubscription(guild.ExternalId, channelB), failuresPerChannel);

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
                fixture.Bus.Publish(new DiscordChannelDeliveryFailed(guild.ExternalId, removingChannel, "50001", Day0.AddDays(day * 2)),
                    TestContext.Current.CancellationToken),
                fixture.Bus.Publish(new DiscordChannelDeliveryFailed(guild.ExternalId, survivingChannel, "50001", Day0),
                    TestContext.Current.CancellationToken));
            await fixture.WaitUntilBusIdle(consumedBefore, published: 2);
        }

        await AppFixture.WaitUntil(async () => await fixture.GuildRepo.GetChannelSubscription(guild.ExternalId, removingChannel) is null);

        var surviving = await WaitForFailureCount(() => fixture.GuildRepo.GetChannelSubscription(guild.ExternalId, survivingChannel), rounds);
        Assert.Equal(rounds, surviving.FailureCount);
    }

    [Fact]
    public async Task SlackFailure_IncrementsCounters()
    {
        var installation = await fixture.SeedInstallation();
        var channelId = installation.ChannelSubscriptions.First().ChannelId;

        await fixture.Bus.Publish(new SlackChannelDeliveryFailed(installation.ExternalId, channelId, "channel_not_found", Day0),
            TestContext.Current.CancellationToken);

        var sub = await WaitForFailureCount(() => fixture.SlackRepo.GetChannelSubscription(installation.ExternalId, channelId), 1);
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
                new SlackChannelDeliveryFailed(installation.ExternalId, channelId, "channel_not_found", Day0.AddDays(day * 2)),
                TestContext.Current.CancellationToken);
            await fixture.WaitUntilBusIdle(consumedBefore);
        }

        await AppFixture.WaitUntil(async () => await fixture.SlackRepo.GetChannelSubscription(installation.ExternalId, channelId) is null);
    }

    [Fact]
    public async Task FailureForAlreadyRemovedChannel_IsIgnored()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);

        var consumedBefore = fixture.ConsumedSoFar;
        await fixture.Bus.Publish(new DiscordChannelDeliveryFailed(guild.ExternalId, "no-such-channel", "50001", Day0),
            TestContext.Current.CancellationToken);
        await fixture.WaitUntilBusIdle(consumedBefore);

        var reloaded = await fixture.GuildRepo.GetInstallation(guild.ExternalId);
        Assert.Single(reloaded.ChannelSubscriptions);
    }

    [Fact]
    public async Task RecordedFailure_IsNeverVisibleHalfWritten()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;

        using var reading = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var halfWritten = 0;
        var reader = Task.Run(async () =>
        {
            while (!reading.IsCancellationRequested)
            {
                if (await fixture.GuildRepo.GetChannelSubscription(guild.ExternalId, channelId) is { FailureCount: > 0, FailingSince: null })
                {
                    Interlocked.Increment(ref halfWritten);
                }
            }
        }, reading.Token);

        for (var i = 0; i < 20; i++)
        {
            var consumedBefore = fixture.ConsumedSoFar;
            await fixture.Bus.Publish(new DiscordChannelDeliveryFailed(guild.ExternalId, channelId, "50001", Day0),
                TestContext.Current.CancellationToken);
            await fixture.WaitUntilBusIdle(consumedBefore);
        }

        await reading.CancelAsync();
        await reader;

        Assert.Equal(0, halfWritten);
    }

    private static async Task<ChannelSubscription> WaitForFailureCount(
        Func<Task<ChannelSubscription?>> read, int expected)
    {
        ChannelSubscription? sub = null;
        await AppFixture.WaitUntil(async () => (sub = await read()) is { } s && s.FailureCount == expected,
            $"Failure count never reached {expected}");
        return sub!;
    }
}
