using FplBot.Data;
using FplBot.Domain;

namespace FplBot.Tests.E2E;

[Collection("App")]
public class FailureStatePersistenceTests(AppFixture fixture) : IAsyncLifetime
{
    private static readonly DateTimeOffset Day0 = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public async ValueTask InitializeAsync() => await fixture.FlushRedisAsync();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task DiscordFailureState_SurvivesRoundTrip()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;

        guild.GetChannel(channelId)!.RecordDeliveryFailure(Day0, "50013");
        guild.GetChannel(channelId)!.RecordDeliveryFailure(Day0.AddDays(1), "50013");
        await fixture.GuildRepo.Save(guild);

        var reloaded = await fixture.GuildRepo.GetInstallation(guild.Id);
        var sub = reloaded.GetChannel(channelId)!;
        Assert.Equal(2, sub.FailureCount);
        Assert.Equal(Day0, sub.FailingSince);
    }

    [Fact]
    public async Task DiscordClearedFailureState_SurvivesRoundTrip()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;

        guild.GetChannel(channelId)!.RecordDeliveryFailure(Day0, "50013");
        await fixture.GuildRepo.Save(guild);

        var reloaded = await fixture.GuildRepo.GetInstallation(guild.Id);
        reloaded.GetChannel(channelId)!.ClearDeliveryFailures();
        await fixture.GuildRepo.Save(reloaded);

        var afterClear = await fixture.GuildRepo.GetInstallation(guild.Id);
        var sub = afterClear.GetChannel(channelId)!;
        Assert.Equal(0, sub.FailureCount);
        Assert.Null(sub.FailingSince);
    }

    [Fact]
    public async Task SlackFailureState_SurvivesRoundTrip()
    {
        var installation = await fixture.SeedInstallation();
        var channelId = installation.ChannelSubscriptions.First().ChannelId;

        installation.GetChannel(channelId)!.RecordDeliveryFailure(Day0, "not_in_channel");
        installation.GetChannel(channelId)!.RecordDeliveryFailure(Day0.AddDays(1), "not_in_channel");
        await fixture.SlackRepo.Save(installation);

        var reloaded = await fixture.SlackRepo.GetInstallation(installation.Id);
        var sub = reloaded.GetChannel(channelId)!;
        Assert.Equal(2, sub.FailureCount);
        Assert.Equal(Day0, sub.FailingSince);
    }

    [Fact]
    public async Task SlackClearedFailureState_SurvivesRoundTrip()
    {
        var installation = await fixture.SeedInstallation();
        var channelId = installation.ChannelSubscriptions.First().ChannelId;

        installation.GetChannel(channelId)!.RecordDeliveryFailure(Day0, "not_in_channel");
        await fixture.SlackRepo.Save(installation);

        var reloaded = await fixture.SlackRepo.GetInstallation(installation.Id);
        reloaded.GetChannel(channelId)!.ClearDeliveryFailures();
        await fixture.SlackRepo.Save(reloaded);

        var afterClear = await fixture.SlackRepo.GetInstallation(installation.Id);
        var sub = afterClear.GetChannel(channelId)!;
        Assert.Equal(0, sub.FailureCount);
        Assert.Null(sub.FailingSince);
    }

    [Fact]
    public async Task PreExistingRecordWithoutFailureFields_DefaultsToZero()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;

        var reloaded = await fixture.GuildRepo.GetInstallation(guild.Id);
        var sub = reloaded.GetChannel(channelId)!;
        Assert.Equal(0, sub.FailureCount);
        Assert.Null(sub.FailingSince);
    }
}
