using FplBot.Data;
using FplBot.Tests.E2E;
using FplBot.Tests.Helpers;

namespace FplBot.Tests.Handlers.DiscordSlashCommands;

[Collection("App")]
public class HelpSlashCommandHandlerTests(AppFixture fixture)
{
    [Fact]
    public async Task NoSubscription_RespondsWithNoSubscriptionsMessage()
    {
        var response = await fixture.AskDiscord("help");
        Assert.Contains("Not subscribing to any events", response.EmbedDescription());
    }

    [Fact]
    public async Task SubscribedWithNoLeagueOrEvents_RespondsWithWarnings()
    {
        var sub = await fixture.SeedGuildSubscription();

        var response = await fixture.AskDiscord("help", guildId: sub.GuildId, channelId: sub.ChannelId);

        Assert.Contains("Not following any FPL leagues", response.EmbedDescription());
        Assert.Contains("No subscriptions", response.EmbedDescription());
    }

    [Fact]
    public async Task SubscribedToEvents_ListsThemAndWhatsMissing()
    {
        var sub = await fixture.SeedGuildSubscription(subscriptions: [EventSubscription.PriceChanges]);

        var response = await fixture.AskDiscord("help", guildId: sub.GuildId, channelId: sub.ChannelId);

        Assert.Contains("PriceChanges", response.EmbedDescription());
        Assert.Contains("Not subscribing", response.EmbedDescription());
    }
}
