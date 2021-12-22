using FakeItEasy;
using Fpl.EventPublishers;
using Microsoft.Extensions.Logging;

namespace FplBot.Tests;

public class PremierLeagueScraperApiTests
{
    [Fact(Skip = "Integration")]
    public async Task GetMatchWithLineups_GetsLineups()
    {
        var client = CreateClient();
        var matchDetails = await client.GetMatchDetails(58972); // https://www.premierleague.com/match/58972 mci - lfc 1-1, match from the past containing lineups
        Assert.NotNull(matchDetails);
        Assert.True(matchDetails.HasTeams());
        Assert.True(matchDetails.HasLineUps());
    }

    [Fact(Skip = "Integration")]
    public async Task GetMatchWithoutLineups_GetsEmptyLineups()
    {
        var client = CreateClient();
        var matchDetails = await client.GetMatchDetails(59272); // https://www.premierleague.com/match/59272 mci - eve (to be played May 23rd 2021)
        Assert.NotNull(matchDetails);
        Assert.True(matchDetails.HasTeams());
        Assert.False(matchDetails.HasLineUps());
    }

    private static PremierLeagueScraperApi CreateClient()
    {
        return new PremierLeagueScraperApi(new HttpClient(),A.Fake<ILogger<PremierLeagueScraperApi>>());
    }
}
