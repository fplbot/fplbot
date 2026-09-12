using FplBot.Data;
using FplBot.Tests.E2E;
using FplBot.Tests.Helpers;

namespace FplBot.Tests.Handlers.DiscordSlashCommands;

[Collection("App")]
public class AddSubscriptionSlashCommandHandlerTests(AppFixture fixture)
{
    [Fact]
    public async Task NoExistingSubscription_CreatesOne()
    {
        var response = await fixture.AskDiscord("subscriptions", optionValue: nameof(EventSubscription.PriceChanges), subCommandName: "add");

        Assert.Contains("Added new subscription", response.EmbedDescription());
        Assert.Contains("PriceChanges", response.EmbedDescription());
    }

    [Fact]
    public async Task AlreadySubscribed_RespondsWithAlreadySubscribing()
    {
        var sub = await fixture.SeedGuildSubscription(subscriptions: [EventSubscription.PriceChanges]);

        var response = await fixture.AskDiscord("subscriptions", optionValue: nameof(EventSubscription.PriceChanges), subCommandName: "add",
            guildId: sub.GuildId, channelId: sub.ChannelId);

        Assert.Contains("Already subscribing", response.EmbedDescription());
    }

    [Fact]
    public async Task ExistingOtherSubscription_AddsNewOne()
    {
        var sub = await fixture.SeedGuildSubscription(subscriptions: [EventSubscription.PriceChanges]);

        var response = await fixture.AskDiscord("subscriptions", optionValue: nameof(EventSubscription.InjuryUpdates), subCommandName: "add",
            guildId: sub.GuildId, channelId: sub.ChannelId);

        Assert.Contains("Updated subscriptions", response.EmbedDescription());
        Assert.Contains("InjuryUpdates", response.EmbedDescription());
    }
}
