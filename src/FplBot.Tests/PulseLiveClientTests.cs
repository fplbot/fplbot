using FakeItEasy;
using Fpl.EventPublishers;
using Microsoft.Extensions.Logging;

namespace FplBot.Tests;

public class PulseLiveClientTests
{
    [Fact]
    public async Task GetMatchWithLineups_GetsLineups()
    {
        var client = CreateClient();
        var matchDetails = await client.GetMatchDetails(58972); // https://www.premierleague.com/match/58972 mci - lfc 1-1, match from the past containing lineups
        Assert.NotNull(matchDetails);
        Assert.True(matchDetails.HasTeams());
        Assert.True(matchDetails.HasLineUps());
    }

    [Fact]
    public async Task GetMatchWithoutLineups_GetsEmptyLineups()
    {
        var client = CreateClient();
        var matchDetails = await client.GetMatchDetails(115838); // https://www.premierleague.com/match/59272 mci - eve (to be played May 23rd 2021)
        Assert.NotNull(matchDetails);
        Assert.True(matchDetails.HasTeams());
        Assert.False(matchDetails.HasLineUps());
    }

    private PulseLiveClient CreateClient()
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("https://footballapi.pulselive.com");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.102 Safari/537.36");
        httpClient.DefaultRequestHeaders.Add("Origin", "https://www.premierleague.com");
        httpClient.DefaultRequestHeaders.Add("Referer", "https://www.premierleague.com/");
        return new PulseLiveClient(httpClient,A.Fake<ILogger<PulseLiveClient>>());
    }
}
