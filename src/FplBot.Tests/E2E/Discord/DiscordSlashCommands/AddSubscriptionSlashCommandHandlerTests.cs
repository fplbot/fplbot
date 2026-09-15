using FplBot.Data;
using FplBot.Domain;
using FplBot.Tests.Helpers;

namespace FplBot.Tests.E2E.Discord.DiscordSlashCommands;

[Collection("App")]
public class AddSubscriptionSlashCommandHandlerTests(AppFixture fixture)
{
    private static string ChannelOf(Installation installation) => installation.ChannelSubscriptions.First().ChannelId;

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
        var seeded = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);

        var response = await fixture.AskDiscord("subscriptions", optionValue: nameof(EventSubscription.PriceChanges), subCommandName: "add",
            guildId: seeded.Id, channelId: ChannelOf(seeded));

        Assert.Contains("Already subscribing", response.EmbedDescription());
    }

    [Fact]
    public async Task ExistingOtherSubscription_AddsNewOne()
    {
        var seeded = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);

        var response = await fixture.AskDiscord("subscriptions", optionValue: nameof(EventSubscription.InjuryUpdates), subCommandName: "add",
            guildId: seeded.Id, channelId: ChannelOf(seeded));

        Assert.Contains("Updated subscriptions", response.EmbedDescription());
        Assert.Contains("InjuryUpdates", response.EmbedDescription());
    }
}
