using FplBot.Tests.E2E;

namespace FplBot.Tests.Handlers.SlackAppMentions;

[Collection("App")]
public class FplTransfersCommandHandlerTests(AppFixture fixture)
{
    [Theory]
    [InlineData("@fplbot transfers")]
    [InlineData("<@UREFQD887> transfers")]
    public async Task GetTransfersHandlerShouldPostTransfers(string input)
    {
        var response = await fixture.AskSlackbot(input);
        Assert.Contains("Transfers", response.Text, StringComparison.InvariantCultureIgnoreCase);
    }

    [Theory]
    [InlineData("<@UREFQD887> transfers 20")]
    public async Task GetTransfersForExplicitGwShouldPostTransfersForGameweek(string input)
    {
        var response = await fixture.AskSlackbot(input);
        Assert.Contains("Transfers", response.Text, StringComparison.InvariantCultureIgnoreCase);
    }

    [Theory]
    [InlineData("@fplbot transfers 1")]
    [InlineData("<@UREFQD887> transfers 1")]
    public async Task GetTransfersHandlerForGw1ShouldPostSpecialMessage(string input)
    {
        var response = await fixture.AskSlackbot(input);
        Assert.Contains("Transfers", response.Text, StringComparison.InvariantCultureIgnoreCase);
    }
}
