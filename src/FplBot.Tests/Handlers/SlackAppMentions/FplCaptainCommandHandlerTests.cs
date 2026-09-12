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
        await fixture.AskSlackbot(input);
        var response = await fixture.SlackCapture.WaitForMessageAsync();
        Assert.StartsWith("💥", response.Text);
    }

    [Theory]
    [InlineData("@fplbot captains 1")]
    [InlineData("<@UREFQD887> captains 1")]
    public async Task GetCaptainsForGameweekShouldPostAllEntryCaptainPicksForThatGameweek(string input)
    {
        await fixture.AskSlackbot(input);
        var response = await fixture.SlackCapture.WaitForMessageAsync();
        Assert.StartsWith("💥", response.Text);
    }

    [Theory]
    [InlineData("@fplbot captains chart")]
    [InlineData("<@UREFQD887> captains chart")]
    public async Task GetCaptainsChartShouldPostAllEntryCaptainPicksInAChartForCurrentGw(string input)
    {
        await fixture.AskSlackbot(input);
        var response = await fixture.SlackCapture.WaitForMessageAsync();
        Assert.StartsWith("📊", response.Text);
    }

    [Theory]
    [InlineData("<@UREFQD887> captains chart 19")]
    [InlineData("<@UREFQD887> captains 19 chart")]
    public async Task GetCaptainsChartShouldPostAllEntryCaptainPicksInAChartForExplicitGw(string input)
    {
        await fixture.AskSlackbot(input);
        var response = await fixture.SlackCapture.WaitForMessageAsync();
        Assert.StartsWith("📊", response.Text);
    }
}
