using FplBot.Tests.E2E;

namespace FplBot.Tests.Handlers.SlackAppMentions;

[Collection("App")]
public class FplCaptainCommandHandlerTests(AppFixture fixture)
{
    [Theory]
    [InlineData("@fplbot captains")]
    [InlineData("<@UREFQD887> captains")]
    public async Task GetCaptainsShouldPostAllEntryCaptainPicks(string input)
    {
        var response = await fixture.AskSlackbot(input);
        Assert.StartsWith("💥", response);
    }

    [Theory]
    [InlineData("@fplbot captains 1")]
    [InlineData("<@UREFQD887> captains 1")]
    public async Task GetCaptainsForGameweekShouldPostAllEntryCaptainPicksForThatGameweek(string input)
    {
        var response = await fixture.AskSlackbot(input);
        Assert.StartsWith("💥", response);
    }

    [Theory]
    [InlineData("@fplbot captains chart")]
    [InlineData("<@UREFQD887> captains chart")]
    public async Task GetCaptainsChartShouldPostAllEntryCaptainPicksInAChartForCurrentGw(string input)
    {
        var response = await fixture.AskSlackbot(input);
        Assert.StartsWith("📊", response);
    }

    [Theory]
    [InlineData("<@UREFQD887> captains chart 19")]
    [InlineData("<@UREFQD887> captains 19 chart")]
    public async Task GetCaptainsChartShouldPostAllEntryCaptainPicksInAChartForExplicitGw(string input)
    {
        var response = await fixture.AskSlackbot(input);
        Assert.StartsWith("📊", response);
    }
}
