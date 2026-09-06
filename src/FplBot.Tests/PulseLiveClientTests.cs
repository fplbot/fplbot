using Fpl.EventPublishers;
using Microsoft.Extensions.Logging;

namespace FplBot.Tests;

public class PulseLiveClientTests
{
    [Fact]
    public async Task GetMatchWithLineups_GetsLineups()
    {
        var client = CreateClient();
        var matchDetails = await client.GetMatchDetails(2645195); // fixture 1, GW1 2026/27, already played
        Assert.NotNull(matchDetails);
        Assert.True(matchDetails!.HasTeams());
        Assert.True(matchDetails.HasLineUps());
    }

    [Fact(Skip = "Integration test. Change to fixture without lineups (future, yet to play)")]
    // [Fact]
    public async Task GetMatchWithoutLineups_GetsEmptyLineups()
    {
        var client = CreateClient();
        var matchDetails = await client.GetMatchDetails(2645198); // https://www.premierleague.com/en/match/2645198/hull-city-vs-manchester-united/overview
        Assert.NotNull(matchDetails);
        Assert.True(matchDetails!.HasTeams());
        Assert.False(matchDetails.HasLineUps());
    }

    private PulseLiveClient CreateClient()
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("https://sdp-prem-prod.premier-league-prod.pulselive.com");
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.102 Safari/537.36");
        httpClient.DefaultRequestHeaders.Add("Origin", "https://www.premierleague.com");
        httpClient.DefaultRequestHeaders.Add("Referer", "https://www.premierleague.com");
        return new PulseLiveClient(httpClient, new LoggerFactory().CreateLogger<PulseLiveClient>());
    }
}
