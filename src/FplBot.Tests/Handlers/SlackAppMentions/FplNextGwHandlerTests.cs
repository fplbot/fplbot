using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Tests.E2E;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.Tests.Handlers.SlackAppMentions;

[Collection("App")]
public class FplNextGwHandlerTests(AppFixture fixture)
{
    [Theory]
    [InlineData("@fplbot next")]
    [InlineData("<@UREFQD887> next")]
    public async Task GetNextGameweekFixtures(string input)
    {
        var fixtureClient = fixture.Services.GetRequiredService<IFixtureClient>();
        A.CallTo(() => fixtureClient.GetFixturesByGameweek(4)).Returns(new List<Fixture>
        {
            new() { HomeTeamId = 1, AwayTeamId = 2, KickOffTime = new DateTime(2026, 9, 12, 14, 0, 0, DateTimeKind.Utc) }
        });

        await fixture.AskSlackbot(input);
        var response = await fixture.SlackCapture.WaitForMessageAsync();

        Assert.Contains("GAMEWEEK 4", response.Text);
        Assert.Contains("ARS-AVL", response.Text);
    }
}
