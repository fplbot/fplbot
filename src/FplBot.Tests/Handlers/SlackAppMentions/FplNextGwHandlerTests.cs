using FplBot.Tests.E2E;
using FplBot.Tests.Helpers;

namespace FplBot.Tests.Handlers.SlackAppMentions;

[Collection("App")]
public class FplNextGwHandlerTests(AppFixture fixture)
{
    [Theory(Skip = "Disable it temporary until ihaztimetofix")]
    [InlineData("@fplbot nextgw")]
    public async Task GetPlayerHandler(string input)
    {
        await fixture.AskSlackbot(input);
        var response = await fixture.SlackCapture.WaitForMessageAsync();
        Assert.NotEmpty(response.AllText());
    }
}
