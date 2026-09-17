using FplBot.Data;
using FplBot.Domain;

namespace FplBot.Tests.E2E.Discord.DiscordSlashCommands;

[Collection("App")]
public class SetupProbeTests(AppFixture fixture) : IAsyncLifetime
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
    public async Task Follow_WhenBotCannotPost_SavesSubscriptionAndWarns()
    {
        var guild = await fixture.SeedGuildInstallation();
        var channelId = guild.ChannelSubscriptions.First().ChannelId;

        var (_, response) = await fixture.AskDiscord("follow", optionValue: "15263", guildId: guild.Id,
            channelId: channelId, appPermissions: DiscordPermissions.None);

        Assert.Contains("Send Messages", response);
        await AppFixture.WaitUntil(async () =>
            (await fixture.GuildRepo.GetChannelSubscription(guild.Id, channelId))?.FollowedLeagueId?.Value == 15263);
    }

    [Fact]
    public async Task Follow_WithoutEmbedLinks_SavesSubscriptionWithoutWarning()
    {
        var guild = await fixture.SeedGuildInstallation();
        var channelId = guild.ChannelSubscriptions.First().ChannelId;

        var (_, response) = await fixture.AskDiscord("follow", optionValue: "15263", guildId: guild.Id,
            channelId: channelId, appPermissions: DiscordPermissions.WithoutEmbedLinks);

        Assert.DoesNotContain("permission", response);
        await AppFixture.WaitUntil(async () =>
            (await fixture.GuildRepo.GetChannelSubscription(guild.Id, channelId))?.FollowedLeagueId?.Value == 15263);
    }

    [Fact]
    public async Task Follow_WhenBotCannotPost_DoesNotCountTowardRemoval()
    {
        var guild = await fixture.SeedGuildInstallation();
        var channelId = guild.ChannelSubscriptions.First().ChannelId;

        await fixture.AskDiscord("follow", optionValue: "15263", guildId: guild.Id,
            channelId: channelId, appPermissions: DiscordPermissions.None);
        await AppFixture.WaitUntil(async () =>
            (await fixture.GuildRepo.GetChannelSubscription(guild.Id, channelId))?.FollowedLeagueId?.Value == 15263);

        var sub = await fixture.GuildRepo.GetChannelSubscription(guild.Id, channelId);
        Assert.Equal(0, sub!.FailureCount);
        Assert.Null(sub.FailingSince);
    }

    [Fact]
    public async Task Follow_WhenBotCanPost_PostsNothingToTheChannel()
    {
        var guild = await fixture.SeedGuildInstallation();
        var channelId = guild.ChannelSubscriptions.First().ChannelId;

        await fixture.AskDiscord("follow", optionValue: "15263", guildId: guild.Id, channelId: channelId);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fixture.DiscordCapture.WaitForMessageAsync(channelId, TimeSpan.FromMilliseconds(300)));
    }

    [Fact]
    public async Task AddSubscription_WhenBotCannotPost_SavesSubscriptionAndWarns()
    {
        var guild = await fixture.SeedGuildInstallation();
        var channelId = guild.ChannelSubscriptions.First().ChannelId;

        var (_, response) = await fixture.AskDiscord("subscriptions", optionValue: "PriceChanges",
            subCommandName: "add", guildId: guild.Id, channelId: channelId,
            appPermissions: DiscordPermissions.None);

        Assert.Contains("Send Messages", response);
        await AppFixture.WaitUntil(async () =>
            (await fixture.GuildRepo.GetChannelSubscription(guild.Id, channelId))?.IsSubscribedTo(FplEvent.PriceChanges) == true);
    }

    [Fact]
    public async Task AddSubscription_WithoutEmbedLinks_SavesSubscriptionWithoutWarning()
    {
        var guild = await fixture.SeedGuildInstallation();
        var channelId = guild.ChannelSubscriptions.First().ChannelId;

        var (_, response) = await fixture.AskDiscord("subscriptions", optionValue: "PriceChanges",
            subCommandName: "add", guildId: guild.Id, channelId: channelId,
            appPermissions: DiscordPermissions.WithoutEmbedLinks);

        Assert.DoesNotContain("permission", response);
        await AppFixture.WaitUntil(async () =>
            (await fixture.GuildRepo.GetChannelSubscription(guild.Id, channelId))?.IsSubscribedTo(FplEvent.PriceChanges) == true);
    }

    [Fact]
    public async Task Help_WhenBotCannotPost_Warns()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;

        var (_, response) = await fixture.AskDiscord("help", guildId: guild.Id, channelId: channelId,
            appPermissions: DiscordPermissions.None);

        Assert.Contains("Send Messages", response);
    }

    [Fact]
    public async Task Help_WithoutEmbedLinks_HasNoWarning()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;

        var (token, _) = await fixture.AskDiscord("help", guildId: guild.Id, channelId: channelId,
            appPermissions: DiscordPermissions.WithoutEmbedLinks);
        var followup = await fixture.DiscordCapture.WaitForFollowupAsync(token);

        Assert.DoesNotContain("Send Messages", followup.Description);
    }

    [Fact]
    public async Task Help_WhenPermissionsUnknown_SaysSoRatherThanAllClear()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;

        var (_, response) = await fixture.AskDiscord("help", guildId: guild.Id, channelId: channelId,
            appPermissions: DiscordPermissions.Unknown);

        Assert.Contains("read my own permissions", response);
    }

    [Fact]
    public async Task Help_WhenBotCanPost_HasNoWarning()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;

        var (token, _) = await fixture.AskDiscord("help", guildId: guild.Id, channelId: channelId);
        var followup = await fixture.DiscordCapture.WaitForFollowupAsync(token);

        Assert.DoesNotContain("Send Messages", followup.Description);
    }
}
