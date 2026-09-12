using FplBot.Tests.E2E;
using FplBot.Tests.Helpers;

namespace FplBot.Tests.Handlers.SlackAppMentions;

[Collection("App")]
public class FplInjuryCommandHandlerTests(AppFixture fixture)
{
    [Theory]
    [InlineData("@fplbot injuries")]
    public async Task GetPlayerHandler(string input)
    {
        var response = await fixture.AskSlackbot(input);
        Assert.NotEmpty(response.AllText());
    }
}
