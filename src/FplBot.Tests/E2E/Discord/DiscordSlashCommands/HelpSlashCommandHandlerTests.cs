using FplBot.Data;
using FplBot.Domain;
using FplBot.Tests.Helpers;

namespace FplBot.Tests.E2E.Discord.DiscordSlashCommands;

[Collection("App")]
public class HelpSlashCommandHandlerTests(AppFixture fixture)
{
    private static string ChannelOf(Installation installation) => installation.ChannelSubscriptions.First().ChannelId;

    [Fact]
    public async Task NoSubscription_RespondsWithNoSubscriptionsMessage()
    {
        var installedGuild = await fixture.SeedGuildInstallation();

        var (token, _) = await fixture.AskDiscord("help", guildId: installedGuild.ExternalId, channelId: Guid.NewGuid().ToString("N"));
        var followup = await fixture.DiscordCapture.WaitForFollowupAsync(token);

        Assert.Contains("Not subscribing to any events", followup.Description);
    }

    [Fact]
    public async Task SubscribedWithNoLeagueOrEvents_RespondsWithWarnings()
    {
        var seeded = await fixture.SeedGuildInstallation();

        var (token, _) = await fixture.AskDiscord("help", guildId: seeded.ExternalId, channelId: ChannelOf(seeded));
        var followup = await fixture.DiscordCapture.WaitForFollowupAsync(token);

        Assert.Contains("Not following any FPL leagues", followup.Description);
        Assert.Contains("No subscriptions", followup.Description);
    }

    [Fact]
    public async Task SubscribedToEvents_ListsThemAndWhatsMissing()
    {
        var seeded = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);

        var (token, _) = await fixture.AskDiscord("help", guildId: seeded.ExternalId, channelId: ChannelOf(seeded));
        var followup = await fixture.DiscordCapture.WaitForFollowupAsync(token);

        Assert.Contains("PriceChanges", followup.Description);
        Assert.Contains("Not subscribing", followup.Description);
    }
}
