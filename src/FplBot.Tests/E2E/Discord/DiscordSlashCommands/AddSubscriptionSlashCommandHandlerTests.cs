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
        var installedGuild = await fixture.SeedGuildInstallation();

        var (token, _) = await fixture.AskDiscord("subscriptions", optionValue: nameof(EventSubscription.PriceChanges), subCommandName: "add",
            guildId: installedGuild.Id, channelId: Guid.NewGuid().ToString("N"));
        var followup = await fixture.DiscordCapture.WaitForFollowupAsync(token);

        Assert.Contains("Added new subscription", followup.Description);
        Assert.Contains("PriceChanges", followup.Description);
    }

    [Fact]
    public async Task AlreadySubscribed_RespondsWithAlreadySubscribing()
    {
        var seeded = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);

        var (token, _) = await fixture.AskDiscord("subscriptions", optionValue: nameof(EventSubscription.PriceChanges), subCommandName: "add",
            guildId: seeded.Id, channelId: ChannelOf(seeded));
        var followup = await fixture.DiscordCapture.WaitForFollowupAsync(token);

        Assert.Contains("Already subscribing", followup.Description);
    }

    [Fact]
    public async Task ExistingOtherSubscription_AddsNewOne()
    {
        var seeded = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);

        var (token, _) = await fixture.AskDiscord("subscriptions", optionValue: nameof(EventSubscription.InjuryUpdates), subCommandName: "add",
            guildId: seeded.Id, channelId: ChannelOf(seeded));
        var followup = await fixture.DiscordCapture.WaitForFollowupAsync(token);

        Assert.Contains("Updated subscriptions", followup.Description);
        Assert.Contains("InjuryUpdates", followup.Description);
    }
}
