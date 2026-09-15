using FplBot.Data;
using FplBot.Domain;
using FplBot.Tests.Helpers;

namespace FplBot.Tests.E2E.Discord.DiscordSlashCommands;

[Collection("App")]
public class RemoveSubscriptionSlashCommandHandlerTests(AppFixture fixture)
{
    private static string ChannelOf(Installation installation) => installation.ChannelSubscriptions.First().ChannelId;

    [Fact]
    public async Task NoExistingSubscription_RespondsWithNothingToRemove()
    {
        var installedGuild = await fixture.SeedGuildInstallation();

        var response = await fixture.AskDiscord("subscriptions", optionValue: nameof(EventSubscription.PriceChanges), subCommandName: "remove",
            guildId: installedGuild.Id, channelId: Guid.NewGuid().ToString("N"));

        Assert.Contains("Did not find any subscription", response.EmbedDescription());
    }

    [Fact]
    public async Task LastSubscriptionWithNoLeague_RemovesSubscriptionEntirely()
    {
        var seeded = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);

        var response = await fixture.AskDiscord("subscriptions", optionValue: nameof(EventSubscription.PriceChanges), subCommandName: "remove",
            guildId: seeded.Id, channelId: ChannelOf(seeded));

        Assert.Contains("Removed subscription to this channel", response.EmbedDescription());
    }

    [Fact]
    public async Task OneOfSeveralSubscriptions_RemovesJustThatOne()
    {
        var seeded = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges, EventSubscription.InjuryUpdates]);

        var response = await fixture.AskDiscord("subscriptions", optionValue: nameof(EventSubscription.PriceChanges), subCommandName: "remove",
            guildId: seeded.Id, channelId: ChannelOf(seeded));

        Assert.Contains($"Unsubscribed from {EventSubscription.PriceChanges}", response.EmbedDescription());
        Assert.Contains("InjuryUpdates", response.EmbedDescription());
    }

    [Fact]
    public async Task SubscribedToAll_RemovingOneSpecificSwitchesToEverythingElse()
    {
        var seeded = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.All]);

        var response = await fixture.AskDiscord("subscriptions", optionValue: nameof(EventSubscription.PriceChanges), subCommandName: "remove",
            guildId: seeded.Id, channelId: ChannelOf(seeded));

        Assert.Contains("No longer subscribing to all events", response.EmbedDescription());
    }
}
