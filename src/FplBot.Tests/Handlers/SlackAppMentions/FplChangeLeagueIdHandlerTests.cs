using Bogus;
using FakeItEasy;
using Fpl.Client.Abstractions;
using Fpl.Client.Models;
using FplBot.Tests.E2E;
using Microsoft.Extensions.DependencyInjection;

namespace FplBot.Tests.Handlers.SlackAppMentions;


[Collection("App")]
public class FplChangeLeagueIdHandlerTests(AppFixture fixture)
{
    private static readonly Faker Faker = new();

    private void SetLeagueFound(int leagueId)
    {
        var leagueClient = fixture.Services.GetRequiredService<ILeagueClient>();
        A.CallTo(() => leagueClient.GetClassicLeague(leagueId, A<int>._, A<bool>._))
            .Returns(new ClassicLeague { Properties = new ClassicLeagueProperties { Name = "Test League" } });
    }

    private void SetLeagueNotFound(int leagueId)
    {
        var leagueClient = fixture.Services.GetRequiredService<ILeagueClient>();
        A.CallTo(() => leagueClient.GetClassicLeague(leagueId, A<int>._, A<bool>._)).Returns((ClassicLeague?)null);
    }

    private static int NewLeagueId() => Faker.Random.Int(100_000, 999_999);

    [Fact]
    public async Task ChangeLeagueIdShouldUpdate()
    {
        var leagueId = NewLeagueId();
        SetLeagueFound(leagueId);

        await fixture.AskSlackbot($"<@UREFQD887> follow {leagueId}");
        var response = await fixture.SlackCapture.WaitForMessageAsync();
        Assert.Contains("Thanks! You're now following", response.Text, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public async Task ChangeToInvalidLeagueIdShouldNotUpdate()
    {
        await fixture.AskSlackbot("<@UREFQD887> follow abc");
        var response = await fixture.SlackCapture.WaitForMessageAsync();
        Assert.Contains("Could not update league to id 'abc'. Make sure it's a single valid number.", response.Text, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public async Task ChangeToNotFoundLeagueIdShouldNotUpdate()
    {
        var leagueId = NewLeagueId();
        SetLeagueNotFound(leagueId);

        await fixture.AskSlackbot($"<@UREFQD887> follow {leagueId}");
        var response = await fixture.SlackCapture.WaitForMessageAsync();
        Assert.Contains($"Could not find league {leagueId} :/ Could you find it at https://fantasy.premierleague.com/leagues/{leagueId}/standings/c ?", response.Text, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public async Task ChangeToMissingArgsProvidesHelpText()
    {
        await fixture.AskSlackbot("<@UREFQD887> follow");
        var response = await fixture.SlackCapture.WaitForMessageAsync();
        Assert.Contains("No leagueId provided. Usage: `@fplbot follow 123`", response.Text, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public async Task ChangeToOtherNotFoundLeagueIdShouldNotUpdate()
    {
        var leagueId = NewLeagueId();
        SetLeagueNotFound(leagueId);

        await fixture.AskSlackbot($"<@UREFQD887> follow {leagueId}");
        var response = await fixture.SlackCapture.WaitForMessageAsync();
        Assert.Contains($"Could not find league {leagueId} :/ Could you find it at https://fantasy.premierleague.com/leagues/{leagueId}/standings/c ?", response.Text, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public async Task HandlesFormattedPhoneNumberLinks()
    {
        var leagueId = NewLeagueId();
        SetLeagueFound(leagueId);

        await fixture.AskSlackbot($"<@UREFQD887> follow <tel:{leagueId}|{leagueId}>");
        var response = await fixture.SlackCapture.WaitForMessageAsync();
        Assert.Contains("Thanks! You're now following", response.Text, StringComparison.InvariantCultureIgnoreCase);
        Assert.Contains($"leagueId: {leagueId}", response.Text, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public async Task HandlesMultipleNumbers()
    {
        await fixture.AskSlackbot("<@UREFQD887> follow 1234 5678");
        var response = await fixture.SlackCapture.WaitForMessageAsync();
        Assert.Contains("Could not update league to id", response.Text, StringComparison.InvariantCultureIgnoreCase);
    }
}
