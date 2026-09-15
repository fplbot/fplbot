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
        var response = await fixture.AskDiscord("help");
        Assert.Contains("Not subscribing to any events", response.EmbedDescription());
    }

    [Fact]
    public async Task SubscribedWithNoLeagueOrEvents_RespondsWithWarnings()
    {
        var seeded = await fixture.SeedGuildInstallation();

        var response = await fixture.AskDiscord("help", guildId: seeded.Id, channelId: ChannelOf(seeded));

        Assert.Contains("Not following any FPL leagues", response.EmbedDescription());
        Assert.Contains("No subscriptions", response.EmbedDescription());
    }

    [Fact]
    public async Task SubscribedToEvents_ListsThemAndWhatsMissing()
    {
        var seeded = await fixture.SeedGuildInstallation(subscriptions: [EventSubscription.PriceChanges]);

        var response = await fixture.AskDiscord("help", guildId: seeded.Id, channelId: ChannelOf(seeded));

        Assert.Contains("PriceChanges", response.EmbedDescription());
        Assert.Contains("Not subscribing", response.EmbedDescription());
    }
}
