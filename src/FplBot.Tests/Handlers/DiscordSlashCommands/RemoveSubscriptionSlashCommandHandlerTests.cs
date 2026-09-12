using FplBot.Data;
using FplBot.Tests.E2E;
using FplBot.Tests.Helpers;

namespace FplBot.Tests.Handlers.DiscordSlashCommands;

[Collection("App")]
public class RemoveSubscriptionSlashCommandHandlerTests(AppFixture fixture)
{
    [Fact]
    public async Task NoExistingSubscription_RespondsWithNothingToRemove()
    {
        var response = await fixture.AskDiscord("subscriptions", optionValue: nameof(EventSubscription.PriceChanges), subCommandName: "remove");

        Assert.Contains("Did not find any subscription", response.EmbedDescription());
    }

    [Fact]
    public async Task LastSubscriptionWithNoLeague_RemovesSubscriptionEntirely()
    {
        var sub = await fixture.SeedGuildSubscription(subscriptions: [EventSubscription.PriceChanges]);

        var response = await fixture.AskDiscord("subscriptions", optionValue: nameof(EventSubscription.PriceChanges), subCommandName: "remove",
            guildId: sub.GuildId, channelId: sub.ChannelId);

        Assert.Contains("Removed subscription to this channel", response.EmbedDescription());
    }

    [Fact]
    public async Task OneOfSeveralSubscriptions_RemovesJustThatOne()
    {
        var sub = await fixture.SeedGuildSubscription(subscriptions: [EventSubscription.PriceChanges, EventSubscription.InjuryUpdates]);

        var response = await fixture.AskDiscord("subscriptions", optionValue: nameof(EventSubscription.PriceChanges), subCommandName: "remove",
            guildId: sub.GuildId, channelId: sub.ChannelId);

        Assert.Contains($"Unsubscribed from {EventSubscription.PriceChanges}", response.EmbedDescription());
        Assert.Contains("InjuryUpdates", response.EmbedDescription());
    }

    [Fact]
    public async Task SubscribedToAll_RemovingOneSpecificSwitchesToEverythingElse()
    {
        var sub = await fixture.SeedGuildSubscription(subscriptions: [EventSubscription.All]);

        var response = await fixture.AskDiscord("subscriptions", optionValue: nameof(EventSubscription.PriceChanges), subCommandName: "remove",
            guildId: sub.GuildId, channelId: sub.ChannelId);

        Assert.Contains("No longer subscribing to all events", response.EmbedDescription());
    }
}
