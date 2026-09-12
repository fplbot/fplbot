using FplBot.Tests.E2E;

namespace FplBot.Tests.Handlers.SlackAppMentions;

[Collection("App")]
public class FplStandingsCommandHandlerTests(AppFixture fixture)
{
    [Theory(Skip = "Humbug før sesongen er i gang?")]
    [InlineData("@fplbot standings")]
    [InlineData("<@UREFQD887> standings")]
    public async Task GetStandings(string input)
    {
        await fixture.AskSlackbot(input);
        var response = await fixture.SlackCapture.WaitForMessageAsync();
        Assert.DoesNotContain("Oops", response.Text);
    }
}
