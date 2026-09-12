using FplBot.Tests.E2E;
using FplBot.Tests.Helpers;

namespace FplBot.Tests.Handlers.SlackAppMentions;

[Collection("App")]
public class FplPlayerCommandHandlerTests(AppFixture fixture)
{
    [Theory]
    [InlineData("@fplbot player haaland")]
    [InlineData("<@UREFQD887> player haaland")]
    public async Task GetPlayerHandler(string input)
    {
        var response = await fixture.AskSlackbot(input);
        Assert.Contains("Haaland", response.AllText());
    }

    [Theory]
    [InlineData("@fplbot player ", "nonexistant")]
    [InlineData("<@UREFQD887> player ", "nonexistant")]
    public async Task GetPlayerHandlerNonPlayer(string input, string player)
    {
        var response = await fixture.AskSlackbot($"{input}{player}");
        Assert.Equal("Couldn't find nonexistant", response.Text);
    }

    [Theory]
    [InlineData("robertson", "Andrew Robertson")]
    [InlineData("van dijk", "Virgil van Dijk")]
    [InlineData("vandijk", "Virgil van Dijk")]
    [InlineData("dijk", "Virgil van Dijk")]
    [InlineData("becker", "Alisson Becker")]
    [InlineData("alisson", "Alisson Becker")]
    public async Task GetPlayer(string input, string expectedPlayer)
    {
        var response = await fixture.AskSlackbot($"<@UREFQD887> player {input}");
        Assert.Contains(expectedPlayer, response.AllText());
    }
}
