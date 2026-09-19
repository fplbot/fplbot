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

        var (token, _) = await fixture.AskDiscord("subscriptions", optionValue: nameof(EventSubscription.PriceChanges), subCommandName: "remove",
            guildId: installedGuild.ExternalId, channelId: Guid.NewGuid().ToString("N"));
        var followup = await fixture.DiscordCapture.WaitForFollowupAsync(token);

        Assert.Contains("Did not find any subscription", followup.Description);
    }

    [Fact]
    public async Task LastSubscriptionWithNoLeague_RemovesSubscriptionEntirely()
    {
        var seeded = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);

        var (token, _) = await fixture.AskDiscord("subscriptions", optionValue: nameof(EventSubscription.PriceChanges), subCommandName: "remove",
            guildId: seeded.ExternalId, channelId: ChannelOf(seeded));
        var followup = await fixture.DiscordCapture.WaitForFollowupAsync(token);

        Assert.Contains("Removed subscription to this channel", followup.Description);
    }

    [Fact]
    public async Task OneOfSeveralSubscriptions_RemovesJustThatOne()
    {
        var seeded = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges, EventSubscription.InjuryUpdates]);

        var (token, _) = await fixture.AskDiscord("subscriptions", optionValue: nameof(EventSubscription.PriceChanges), subCommandName: "remove",
            guildId: seeded.ExternalId, channelId: ChannelOf(seeded));
        var followup = await fixture.DiscordCapture.WaitForFollowupAsync(token);

        Assert.Contains($"Unsubscribed from {EventSubscription.PriceChanges}", followup.Description);
        Assert.Contains("InjuryUpdates", followup.Description);
    }

    [Fact]
    public async Task SubscribedToAll_RemovingOneSpecificSwitchesToEverythingElse()
    {
        var seeded = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.All]);

        var (token, _) = await fixture.AskDiscord("subscriptions", optionValue: nameof(EventSubscription.PriceChanges), subCommandName: "remove",
            guildId: seeded.ExternalId, channelId: ChannelOf(seeded));
        var followup = await fixture.DiscordCapture.WaitForFollowupAsync(token);

        Assert.Contains("No longer subscribing to all events", followup.Description);
    }
}
