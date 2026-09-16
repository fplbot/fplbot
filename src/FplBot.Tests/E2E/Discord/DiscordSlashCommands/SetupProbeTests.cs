using FplBot.Data;
using FplBot.Domain;

namespace FplBot.Tests.E2E.Discord.DiscordSlashCommands;

[Collection("App")]
public class SetupProbeTests(AppFixture fixture) : IAsyncLifetime
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
    public async Task Follow_WhenChannelBlocked_SavesSubscriptionAndWarns()
    {
        var guild = await fixture.SeedGuildInstallation();
        var channelId = guild.ChannelSubscriptions.First().ChannelId;
        fixture.DiscordChannelFails(channelId, 50013);

        var response = await fixture.AskDiscord("follow", optionValue: "15263", guildId: guild.Id, channelId: channelId);

        Assert.Contains("permission", response, StringComparison.OrdinalIgnoreCase);
        var sub = await fixture.GuildRepo.GetChannelSubscription(guild.Id, channelId);
        Assert.NotNull(sub);
        Assert.Equal(15263, (int)sub.FollowedLeagueId!.Value);
    }

    [Fact]
    public async Task Follow_WhenChannelBlocked_DoesNotCountTowardRemoval()
    {
        var guild = await fixture.SeedGuildInstallation();
        var channelId = guild.ChannelSubscriptions.First().ChannelId;
        fixture.DiscordChannelFails(channelId, 50013);

        await fixture.AskDiscord("follow", optionValue: "15263", guildId: guild.Id, channelId: channelId);

        var sub = await fixture.GuildRepo.GetChannelSubscription(guild.Id, channelId);
        Assert.Equal(0, sub!.FailureCount);
        Assert.Null(sub.FailingSince);
    }

    [Fact]
    public async Task Follow_WhenChannelWorks_PostsConfirmationToChannel()
    {
        var guild = await fixture.SeedGuildInstallation();
        var channelId = guild.ChannelSubscriptions.First().ChannelId;

        await fixture.AskDiscord("follow", optionValue: "15263", guildId: guild.Id, channelId: channelId);

        var posted = await fixture.DiscordCapture.WaitForMessageAsync(channelId);
        Assert.NotNull(posted);
    }

    [Fact]
    public async Task AddSubscription_WhenChannelBlocked_SavesSubscriptionAndWarns()
    {
        var guild = await fixture.SeedGuildInstallation();
        var channelId = guild.ChannelSubscriptions.First().ChannelId;
        fixture.DiscordChannelFails(channelId, 50001);

        var response = await fixture.AskDiscord("subscriptions", optionValue: "PriceChanges",
            subCommandName: "add", guildId: guild.Id, channelId: channelId);

        Assert.Contains("access", response, StringComparison.OrdinalIgnoreCase);
        var sub = await fixture.GuildRepo.GetChannelSubscription(guild.Id, channelId);
        Assert.True(sub!.IsSubscribedTo(FplEvent.PriceChanges));
    }

    [Fact]
    public async Task Help_WhenChannelBlocked_Warns()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;
        fixture.DiscordChannelFails(channelId, 50013);

        var response = await fixture.AskDiscord("help", guildId: guild.Id, channelId: channelId);

        Assert.Contains("unable to post", response, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("permission", response, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Help_WhenChannelWorks_HasNoWarning()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;

        var response = await fixture.AskDiscord("help", guildId: guild.Id, channelId: channelId);

        Assert.DoesNotContain("unable to post", response, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Help_WhenChannelRecovers_ClearsCounters()
    {
        var guild = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);
        var channelId = guild.ChannelSubscriptions.First().ChannelId;
        guild.GetChannel(channelId)!.RecordDeliveryFailure(Day0);
        await fixture.GuildRepo.Save(guild);

        await fixture.AskDiscord("help", guildId: guild.Id, channelId: channelId);

        var sub = await fixture.GuildRepo.GetChannelSubscription(guild.Id, channelId);
        Assert.Equal(0, sub!.FailureCount);
    }
}
