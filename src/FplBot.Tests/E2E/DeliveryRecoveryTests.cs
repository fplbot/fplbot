using FplBot.Data;
using FplBot.Domain;
using FplBot.Messaging.Contracts.Commands.v1;
using FplBot.Messaging.Contracts.Events.v1;

namespace FplBot.Tests.E2E;

[Collection("App")]
public class DeliveryRecoveryTests(AppFixture fixture) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        fixture.SlackCapture.Reset();
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
    public async Task SlackSuccessfulPost_ClearsCounters()
    {
        var installation = await fixture.SeedInstallation();
        var channelId = installation.ChannelSubscriptions.First().ChannelId;

        fixture.SlackChannelFails(channelId, "channel_not_found");
        await fixture.Bus.Publish(new PublishToSlack(installation.Id, channelId, "one"), TestContext.Current.CancellationToken);
        await WaitForFailureCount(() => fixture.SlackRepo.GetChannelSubscription(installation.Id, channelId), 1);

        fixture.RecoverSlackChannel(channelId);
        await fixture.Bus.Publish(new PublishToSlack(installation.Id, channelId, "two"), TestContext.Current.CancellationToken);

        await WaitForFailureCount(() => fixture.SlackRepo.GetChannelSubscription(installation.Id, channelId), 0);
    }

    [Fact]
    public async Task DiscordSuccessfulPost_ClearsCounters()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;

        fixture.DiscordChannelFails(channelId, 50001);
        await fixture.Bus.Publish(new PublishToGuildChannel(guild.Id, channelId, "one"), TestContext.Current.CancellationToken);
        await WaitForFailureCount(() => fixture.GuildRepo.GetChannelSubscription(guild.Id, channelId), 1);

        fixture.RecoverDiscordChannel(channelId);
        await fixture.Bus.Publish(new PublishToGuildChannel(guild.Id, channelId, "two"), TestContext.Current.CancellationToken);

        await WaitForFailureCount(() => fixture.GuildRepo.GetChannelSubscription(guild.Id, channelId), 0);
    }

    private static async Task WaitForFailureCount(Func<Task<ChannelSubscription?>> read, int expected)
    {
        await AppFixture.WaitUntil(async () => await read() is { } sub && sub.FailureCount == expected,
            $"Failure count never reached {expected}");
    }
}
