using FplBot.Tests.E2E;

namespace FplBot.Tests.Handlers.SlackAppMentions;

[Collection("App")]
public class FplPlayerCommandHandlerTests(AppFixture fixture)
{
    [Theory]
    [InlineData("@fplbot player salah")]
    [InlineData("<@UREFQD887> player salah")]
    public async Task GetPlayerHandler(string input)
    {
        var response = await fixture.AskSlackbot(input);
        Assert.Contains("Found matching player for salah", response);
    }

    [Theory]
    [InlineData("@fplbot player ", "nonexistant")]
    [InlineData("<@UREFQD887> player ", "nonexistant")]
    public async Task GetPlayerHandlerNonPlayer(string input, string player)
    {
        var response = await fixture.AskSlackbot($"{input}{player}");
        Assert.Equal("Found no matching player for nonexistant: ", response);
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
        Assert.Equal($"Found matching player for {input}: {expectedPlayer}", response);
    }
}
